// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS;

[Generator]
public class BindgenSourceGenerator : ISourceGenerator
{
    private string _programFilePath = null!;

    public void Initialize(GeneratorInitializationContext context)
    {
       _programFilePath = GetProgramPath();
       context.RegisterForSyntaxNotifications(() => new BindgenSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var targets = Targets(context);
        if (targets == null)
        {
            return;
        }

        TargetsGenerateCode(context, targets.Value);
    }

    private void TargetsGenerateCode(
        GeneratorExecutionContext context, ImmutableArray<BindgenTarget> targets)
    {
        foreach (var target in targets)
        {
            TargetGenerateCode(context, target);
        }
    }

    private void TargetGenerateCode(
        GeneratorExecutionContext context, BindgenTarget target)
    {
        var configuration = CreateConfiguration(target);
        WriteConfiguration(target.ConfigurationFilePath, configuration);
        var shellOutput = Bindgen(target);
        if (shellOutput.ExitCode != 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.BindgenFailed,
                Location.None,
                target.ClassName,
                target.ConfigurationFilePath,
                target.OutputLogFilePath));
            return;
        }

        var sourceCodeFilePath = configuration.WriteCSharpCode?.OutputFilePath;
        if (string.IsNullOrEmpty(sourceCodeFilePath))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.BindgenNoSourceCode,
                Location.None,
                target.ClassName,
                target.ConfigurationFilePath,
                target.OutputLogFilePath));
            return;
        }

        var sourceCode = File.ReadAllText(sourceCodeFilePath);
        context.AddSource(target.ClassName + ".g.cs", sourceCode);
    }

    private Terminal.ShellOutput Bindgen(BindgenTarget target)
    {
        var command = $"{_programFilePath} -c {target.ConfigurationFilePath}";
        var shellOutput = command.ExecuteShell(workingDirectory: target.WorkingDirectory);
        File.WriteAllText(target.OutputLogFilePath, shellOutput.Output);
        return shellOutput;
    }

    private static void WriteConfiguration(string filePath, BindgenConfiguration configuration)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var configurationJsonString = JsonSerializer.Serialize(configuration, jsonSerializerOptions);
        File.WriteAllText(filePath, configurationJsonString);
    }

    private ImmutableArray<BindgenTarget>? Targets(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not BindgenSyntaxReceiver bindgenSyntaxReceiver)
        {
            return null;
        }

        if (!ValidateProgramExists(context))
        {
            return null;
        }

        if (!ValidateTargets(context, bindgenSyntaxReceiver.Targets))
        {
            return null;
        }

        return bindgenSyntaxReceiver.Targets.ToImmutableArray();
    }

    private BindgenConfiguration CreateConfiguration(BindgenTarget target)
    {
        var result = new BindgenConfiguration
        {
            ReadCCode = new ReadCodeCConfiguration
            {
                WorkingDirectory = target.WorkingDirectory
            },
            WriteCSharpCode = new WriteCodeCSharpConfiguration
            {
                WorkingDirectory = target.WorkingDirectory
            }
        };
        return result;
    }

    private bool ValidateTargets(
        GeneratorExecutionContext context, List<BindgenTarget> targets)
    {
        if (targets.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BindgenClassNotFound, Location.None));
            return false;
        }

        return true;
    }

    private bool ValidateProgramExists(GeneratorExecutionContext context)
    {
        if (File.Exists(_programFilePath))
        {
            return true;
        }

        context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BindgenProgramNotFound, Location.None, _programFilePath));
        return false;
    }

    private string GetProgramPath()
    {
        return "/Users/lstranks/Programming/c2cs/bin/C2CS.CommandLineInterface/Debug/net6.0/C2CS.CommandLineInterface";

        // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        // {
        //     var (output, _) = "Show-Command -Name \"c2cs\"".ShellCaptureOutput();
        //     return output;
        // }
        // else
        // {
        //     var (output, _) = "command -v c2cs".ShellCaptureOutput();
        //     return output;
        // }
    }
}
