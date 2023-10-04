﻿using System.CommandLine;
using Octopus.Tentacle.Contracts;
using Octopus.Tentacle.Diagnostics;
using Octopus.Tentacle.Scripts;
using Octopus.Tentacle.Util;

namespace Octopus.Tentacle.Kubernetes.ScriptRunner.Commands;

public class ExecuteScriptCommand : RootCommand
{
    private readonly IShell shell;

    public ExecuteScriptCommand()
        : base("Executes the script found in the work directory for the script ticket")
    {
        if (PlatformDetection.IsRunningOnWindows)
            shell = new PowerShell();
        else
            shell = new Bash();

        var scriptPathOption = new Option<string?>(
            name: "--script",
            description: "The path to the script file to execute");
        AddOption(scriptPathOption);

        var scriptArgsOption = new Option<string[]>(
            name: "--args",
            description: "The arguments to be passed to the script")
        {
            AllowMultipleArgumentsPerToken = true
        };
        AddOption(scriptArgsOption);

        this.SetHandler(async context =>
        {
            var scriptPath = context.ParseResult.GetValueForOption(scriptPathOption);
            var scriptArgs = context.ParseResult.GetValueForOption(scriptArgsOption);
            var token = context.GetCancellationToken();

            var exitCode = await ExecuteScript(scriptPath!, scriptArgs, token);

            context.ExitCode = exitCode;
        });
    }

    private async Task<int> ExecuteScript(string scriptPath, string[]? scriptArgs, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var workingDirectory = Path.GetDirectoryName(scriptPath);

        var workspace = new BashScriptWorkspace(
            workingDirectory!,
            new OctopusPhysicalFileSystem(new SystemLog()),
            new SensitiveValueMasker());

        var log = workspace.CreateLog();
        var writer = log.CreateWriter();

        scriptArgs ??= Array.Empty<string>();

        try
        {
            var exitCode = SilentProcessRunner.ExecuteCommand(
                shell.GetFullPath(),
                shell.FormatCommandArguments(scriptPath, scriptArgs, false),
                workingDirectory!,
                output => writer.WriteOutput(ProcessOutputSource.Debug, output),
                output => writer.WriteOutput(ProcessOutputSource.StdOut, output),
                output => writer.WriteOutput(ProcessOutputSource.StdErr, output),
                cancellationToken);

            return exitCode;
        }
        catch (Exception ex)
        {
            writer.WriteOutput(ProcessOutputSource.StdErr, "An exception was thrown when invoking " + shell.GetFullPath() + ": " + ex.Message);
            writer.WriteOutput(ProcessOutputSource.StdErr, ex.ToString());

            return ScriptExitCodes.PowershellInvocationErrorExitCode;
        }
    }
}