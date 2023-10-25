﻿using System;
using System.Threading;
using Octopus.Diagnostics;
using Octopus.Tentacle.Contracts.ScriptServiceV3Alpha;

namespace Octopus.Tentacle.Scripts
{
    class LocalShellScriptExecutor : IScriptExecutor
    {
        readonly IShell shell;
        readonly ISystemLog log;

        public LocalShellScriptExecutor(IShell shell, ISystemLog log)
        {
            this.shell = shell;
            this.log = log;
        }

        public IRunningScript ExecuteOnBackgroundThread(StartScriptCommandV3Alpha command, IScriptWorkspace workspace, ScriptStateStore? scriptStateStore, CancellationToken cancellationToken)
        {
            if (command.ExecutionContext is not LocalShellScriptExecutionContext)
                throw new InvalidOperationException($"Cannot execute start script command as the execution context is not of type {nameof(LocalShellScriptExecutionContext)}.");

            var runningScript = new RunningScript(shell, workspace,  scriptStateStore, workspace.CreateLog(), command.TaskId, cancellationToken, log);

            var thread = new Thread(runningScript.Execute) { Name = $"Executing {shell.Name} script for " + command.ScriptTicket.TaskId };
            thread.Start();

            return runningScript;
        }
    }
}