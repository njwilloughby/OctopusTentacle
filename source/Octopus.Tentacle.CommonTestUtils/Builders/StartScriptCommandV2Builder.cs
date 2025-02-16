using System;
using System.Collections.Generic;
using System.Text;
using Octopus.Tentacle.Contracts;
using Octopus.Tentacle.Contracts.ScriptServiceV2;
using Octopus.Tentacle.Scripts;
using Octopus.Tentacle.Tests.Integration.Util.Builders;
using Octopus.Tentacle.Util;

namespace Octopus.Tentacle.CommonTestUtils.Builders
{
    public class StartScriptCommandV2Builder
    {
        readonly List<ScriptFile> files = new List<ScriptFile>();
        readonly List<string> arguments = new List<string>();
        readonly Dictionary<ScriptType, string> additionalScripts = new Dictionary<ScriptType, string>();
        StringBuilder scriptBody = new StringBuilder(string.Empty);
        ScriptIsolationLevel isolation = ScriptIsolationLevel.NoIsolation;
        TimeSpan scriptIsolationMutexTimeout = ScriptIsolationMutex.NoTimeout;
        string scriptIsolationMutexName = nameof(RunningScript);
        string taskId = Guid.NewGuid().ToString();
        ScriptTicket scriptTicket = new ScriptTicket(Guid.NewGuid().ToString());
        TimeSpan? durationStartScriptCanWaitForScriptToFinish;

        public StartScriptCommandV2Builder WithScriptBody(string scriptBody)
        {
            this.scriptBody = new StringBuilder(scriptBody);
            return this;
        }

        public StartScriptCommandV2Builder WithScriptBodyForCurrentOs(string windowsScript, string bashScript)
        {
            this.scriptBody = new StringBuilder(PlatformDetection.IsRunningOnWindows ? windowsScript : bashScript);
            return this;
        }

        public StartScriptCommandV2Builder WithScriptBody(ScriptBuilder scriptBuilder)
        {
            scriptBody = new StringBuilder(scriptBuilder.BuildForCurrentOs());
            return this;
        }

        public StartScriptCommandV2Builder WithScriptBody(Action<ScriptBuilder> builderFunc)
        {
            var scriptBuilder = new ScriptBuilder();
            builderFunc(scriptBuilder);
            return WithScriptBody(scriptBuilder);
        }

        public StartScriptCommandV2Builder WithAdditionalScriptTypes(ScriptType scriptType, string scriptBody)
        {
            additionalScripts.Add(scriptType, scriptBody);
            return this;
        }

        public StartScriptCommandV2Builder WithIsolation(ScriptIsolationLevel isolation)
        {
            this.isolation = isolation;
            return this;
        }

        public StartScriptCommandV2Builder WithFiles(params ScriptFile[] files)
        {
            if (files != null)
                this.files.AddRange(files);

            return this;
        }

        public StartScriptCommandV2Builder WithArguments(params string[] arguments)
        {
            if (arguments != null)
                this.arguments.AddRange(arguments);

            return this;
        }

        public StartScriptCommandV2Builder WithMutexTimeout(TimeSpan scriptIsolationMutexTimeout)
        {
            this.scriptIsolationMutexTimeout = scriptIsolationMutexTimeout;
            return this;
        }

        public StartScriptCommandV2Builder WithMutexName(string name)
        {
            scriptIsolationMutexName = name;
            return this;
        }

        public StartScriptCommandV2Builder WithTaskId(string taskId)
        {
            this.taskId = taskId;
            return this;
        }

        public StartScriptCommandV2Builder WithScriptTicket(ScriptTicket scriptTicket)
        {
            this.scriptTicket = scriptTicket;
            return this;
        }

        public StartScriptCommandV2Builder WithDurationStartScriptCanWaitForScriptToFinish(TimeSpan? duration)
        {
            this.durationStartScriptCanWaitForScriptToFinish = duration;
            return this;
        }

        public StartScriptCommandV2 Build()
            => new StartScriptCommandV2(scriptBody.ToString(),
                isolation,
                scriptIsolationMutexTimeout,
                scriptIsolationMutexName,
                arguments.ToArray(),
                taskId,
                scriptTicket,
                durationStartScriptCanWaitForScriptToFinish,
                additionalScripts,
                files.ToArray());
    }
}