// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Data;
using Microsoft.CodeAnalysis;

namespace C2CS.SourceGenerator;

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
        var configurations = Targets(context);
        if (configurations == null)
        {
            return;
        }

        ExecuteTargets(context, configurations.Value);
    }

    private void ExecuteTargets(
        GeneratorExecutionContext context,
        ImmutableArray<BindgenTarget> targets)
    {
        foreach (var target in targets)
        {
            ExecuteTarget(context, target);
        }
    }

    private void ExecuteTarget(
        GeneratorExecutionContext context,
        BindgenTarget target)
    {
        if (target.Configuration.ReadCCode == null || target.Configuration.WriteCSharpCode == null)
        {
            return;
        }

        var inputFilePath = target.Configuration.ReadCCode.InputFilePath;
        if (string.IsNullOrEmpty(inputFilePath) || !File.Exists(inputFilePath))
        {
            return;
        }

        var className = target.Configuration.WriteCSharpCode!.ClassName;
        var outputConfigurationFilePath = Path.Combine(target.OutputDirectory, $"{className}.json");
        var outputLogFilePath = Path.Combine(target.OutputDirectory, $"{className}.log");
        WriteConfiguration(outputConfigurationFilePath, target.Configuration);

        var shellOutput = Bindgen(target.WorkingDirectory, outputConfigurationFilePath, outputLogFilePath);
        if (shellOutput.ExitCode != 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.BindgenFailed,
                Location.None,
                className,
                outputConfigurationFilePath,
                outputLogFilePath));
            return;
        }

        var sourceCodeFilePath = target.Configuration.WriteCSharpCode.OutputFilePath;
        var sourceCodeFileInfo = new FileInfo(sourceCodeFilePath!);
        if (!sourceCodeFileInfo.Exists)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.BindgenNoSourceCode,
                Location.None,
                className,
                outputConfigurationFilePath,
                outputLogFilePath));
            return;
        }

        if (target.IsEnabledAddAsSource)
        {
            var sourceCode = File.ReadAllText(sourceCodeFileInfo.FullName);
            var fileName = Path.GetFileName(sourceCodeFileInfo.FullName);
            context.AddSource(fileName, sourceCode);
            sourceCodeFileInfo.Delete();
        }
    }

    private Terminal.ShellOutput Bindgen(string workingDirectory, string configurationFilePath, string outputLogFilePath)
    {
        var command = $"-c {configurationFilePath}";
        var shellOutput = command.ExecuteShell(workingDirectory, _programFilePath);
        File.WriteAllText(outputLogFilePath, shellOutput.Output);
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
        if (context.SyntaxContextReceiver is not BindgenSyntaxReceiver bindgenSyntaxReceiver)
        {
            return null;
        }

        if (!ValidateProgramExists(context))
        {
            return null;
        }

        var targets = bindgenSyntaxReceiver.Targets.ToImmutableArray();
        if (!ValidateTargets(context, targets))
        {
            return null;
        }

        return targets;
    }

    // private static BindgenConfiguration CreateConfiguration(
    //     string workingDirectory, string outputFilePath, BindgenTargetConfiguration configuration)
    // {
    //     var configurationPlatforms = new Dictionary<string, ReadCodeCConfigurationPlatform?>();
    //
    //     var read = new ReadCodeCConfiguration
    //     {
    //         WorkingDirectory = workingDirectory,
    //         InputFilePath = configuration.ReadCodeCInputFilePath,
    //         ConfigurationPlatforms = configurationPlatforms,
    //         IsEnabledSystemDeclarations = configuration.IsEnabledSystemDeclarations
    //     };
    //
    //     var write = new WriteCodeCSharpConfiguration
    //     {
    //         WorkingDirectory = workingDirectory,
    //         LibraryName = configuration.LibraryName,
    //         ClassName = configuration.WriteCodeCSharpClassName,
    //         NamespaceName = configuration.WriteCodeCSharpNamespaceName,
    //         OutputFilePath = outputFilePath
    //     };
    //
    //     var result = new BindgenConfiguration
    //     {
    //         ReadCCode = read,
    //         WriteCSharpCode = write,
    //         InputOutputFileDirectory = configuration.InputOutputFileDirectory
    //     };
    //
    //     foreach (var attribute in configuration.Attributes.TargetPlatforms)
    //     {
    //         var exists = configurationPlatforms.TryGetValue(attribute.Name, out var configurationPlatform);
    //         if (!exists)
    //         {
    //             configurationPlatform = new ReadCodeCConfigurationPlatform();
    //             configurationPlatforms.Add(attribute.Name, configurationPlatform);
    //         }
    //
    //         configurationPlatform!.Frameworks = attribute.Frameworks.Cast<string?>().ToImmutableArray();
    //     }
    //
    //     var functionNamesAllowed = ImmutableArray.CreateBuilder<string?>();
    //     foreach (var attribute in configuration.Attributes.Functions)
    //     {
    //         functionNamesAllowed.Add(attribute.Name);
    //     }
    //
    //     read.FunctionNamesAllowed = functionNamesAllowed.ToImmutable();
    //
    //     return result;
    // }

    private bool ValidateTargets(
        GeneratorExecutionContext context, ImmutableArray<BindgenTarget> targets)
    {
        if (targets.Length == 0)
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
        return "/Users/lstranks/Programming/c2cs/bin/C2CS/Debug/net6.0/C2CS";

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

    private string GetTemporaryAppDirectory()
    {
        var directoryName = Assembly.GetEntryAssembly()!.GetName().Name;
        var temporaryPath = Path.Combine(Path.GetTempPath(), directoryName);
        if (!Directory.Exists(temporaryPath))
        {
            Directory.CreateDirectory(temporaryPath);
        }

        return temporaryPath;
    }
}
