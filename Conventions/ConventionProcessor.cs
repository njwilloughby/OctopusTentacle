using System;
using System.Collections.Generic;
using System.Linq;
using Octopus.Shared.Activities;
using Octopus.Shared.Contracts;
using Octopus.Shared.Util;

namespace Octopus.Shared.Conventions
{
    public class ConventionProcessor : IConventionProcessor
    {
        readonly IEnumerable<IConvention> conventions;

        public ConventionProcessor(IEnumerable<IConvention> conventions)
        {
            this.conventions = conventions;
        }

        public void RunConventions(IConventionContext context)
        {
            EvaluateVariables(context, context.Log);

            try
            {
                // Now run the "conventions", for example: Deploy.ps1 scripts, XML configuration, and so on
                RunInstallConventions(context);

                // Run cleanup for rollback conventions, for example: delete DeployFailed.ps1 script
                RunRollbackCleanup(context);
            }
            catch (Exception ex)
            {
                context.Log.Error(ex);
                context.Log.Error("Running rollback conventions...");

                ex = ex.GetRootError();
                context.Variables.Set(SpecialVariables.LastError, ex.ToString());
                context.Variables.Set(SpecialVariables.LastErrorMessage, ex.Message);

                // Rollback conventions include tasks like DeployFailed.ps1
                RunRollbackConventions(context);

                // Run cleanup for rollback conventions, for example: delete DeployFailed.ps1 script
                RunRollbackCleanup(context);

                throw;
            }
        }

        void RunInstallConventions(IConventionContext context)
        {
            Run<IInstallationConvention>(context, (c, ctx) => c.Install(ctx));
        }

        void RunRollbackConventions(IConventionContext context)
        {
            Run<IRollbackConvention>(context, (c, ctx) => c.Rollback(ctx));
        }

        void RunRollbackCleanup(IConventionContext context)
        {
            Run<IRollbackConvention>(context, (c, ctx) => c.Cleanup(ctx));
        }

        void Run<TConvention>(IConventionContext context, Action<TConvention, IConventionContext> conventionCallback) where TConvention : IConvention
        {
            var conventionsToRun = 
                conventions.OfType<TConvention>()
                .OrderBy(p => p.Priority)
                .ToList();

            foreach (var convention in conventionsToRun)
            {
                var childContext = context.ScopeTo(convention);
                conventionCallback(convention, childContext);
            }
        }

        static void EvaluateVariables(IConventionContext context, IActivityLog log)
        {
            context.Variables.Set(SpecialVariables.OriginalPackageDirectoryPath, context.PackageContentsDirectoryPath);

            if (context.Variables.GetFlag(SpecialVariables.PrintVariables, false))
            {
                PrintVariables("The following variables are available:", context.Variables, log);
            }

            if (!context.Variables.GetFlag(SpecialVariables.NoVariableTokenReplacement, false))
            {
                new VariableEvaluator().Evaluate(context.Variables);

                if (context.Variables.GetFlag(SpecialVariables.PrintEvaluatedVariables, false))
                {
                    log.Debug("Variables have been evaluated.");
                    PrintVariables("The following evaluated variables are available:", context.Variables, log);
                }
            }
        }

        static void PrintVariables(string message, VariableDictionary variables, IActivityLog log)
        {
            log.Debug(message);

            foreach (var variable in variables.AsList().OrderBy(v => v.Name))
            {
                if (SpecialVariables.IsSecret(variable.Name))
                    continue;

                log.DebugFormat(" - [{0}] = '{1}'", variable.Name, variable.Value);
            }
        }
    }
}