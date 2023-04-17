using System;
using System.IO;
using Octopus.Tentacle.Configuration;
using Octopus.Tentacle.Contracts;
using Octopus.Tentacle.Security;
using Octopus.Tentacle.Util;

namespace Octopus.Tentacle.Scripts
{
    public class ScriptWorkspaceFactory : IScriptWorkspaceFactory
    {
        readonly IOctopusFileSystem fileSystem;
        readonly IHomeConfiguration home;

        public ScriptWorkspaceFactory(IOctopusFileSystem fileSystem, IHomeConfiguration home)
        {
            if (home.ApplicationSpecificHomeDirectory == null)
                throw new ArgumentException($"{GetType().Name} cannot function without the HomeDirectory configured.", nameof(home));

            this.fileSystem = fileSystem;
            this.home = home;
        }

        public IScriptWorkspace GetWorkspace(ScriptTicket ticket)
        {
            if (!PlatformDetection.IsRunningOnWindows)
                return new BashScriptWorkspace(FindWorkingDirectory(ticket), fileSystem);

            return new ScriptWorkspace(FindWorkingDirectory(ticket), fileSystem);
        }

        public IScriptWorkspace PrepareWorkspace(StartScriptCommand command, ScriptTicket ticket)
        {
            var workspace = GetWorkspace(ticket);
            workspace.IsolationLevel = command.Isolation;
            workspace.ScriptMutexAcquireTimeout = command.ScriptIsolationMutexTimeout;
            workspace.ScriptArguments = command.Arguments;
            workspace.ScriptMutexName = command.IsolationMutexName;

            if (PlatformDetection.IsRunningOnNix || PlatformDetection.IsRunningOnMac)
            {
                //TODO: This could be better
                workspace.BootstrapScript(command.Scripts.ContainsKey(ScriptType.Bash)
                    ? command.Scripts[ScriptType.Bash]
                    : command.ScriptBody);
            }
            else
            {
                workspace.BootstrapScript(command.ScriptBody);
            }

            command.Files.ForEach(file => SaveFileToDisk(workspace, file));

            return workspace;
        }

        void SaveFileToDisk(IScriptWorkspace workspace, ScriptFile scriptFile)
        {
            if (scriptFile.EncryptionPassword == null)
            {
                scriptFile.Contents.Receiver().SaveTo(workspace.ResolvePath(scriptFile.Name));
            }
            else
            {
                scriptFile.Contents.Receiver().Read(stream =>
                {
                    using var reader = new StreamReader(stream);
                    fileSystem.WriteAllBytes(workspace.ResolvePath(scriptFile.Name), new AesEncryption(scriptFile.EncryptionPassword).Encrypt(reader.ReadToEnd()));
                });
            }
        }


        string FindWorkingDirectory(ScriptTicket ticket)
        {
            var work = fileSystem.GetFullPath(Path.Combine(home.HomeDirectory ?? "", "Work", ticket.TaskId));
            fileSystem.EnsureDirectoryExists(work);
            return work;
        }
    }
}