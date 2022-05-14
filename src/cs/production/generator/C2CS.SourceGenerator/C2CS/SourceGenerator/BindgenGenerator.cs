// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Contexts.WriteCodeCSharp.Data;
using C2CS.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Hosting;

namespace C2CS.SourceGenerator;

[Generator]
public class BindgenGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var bindgenTargetsIncremental = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsBindgenTarget(node),
                transform: static (context, _) => BindgenTarget(context))
            .Where(static x => x is not null);

        var bindgenTargets = bindgenTargetsIncremental.Collect();

        context.RegisterSourceOutput(
            bindgenTargets,
            static (context, bindgenTargets) => ExecuteTargets(context, bindgenTargets));
    }

    private static bool IsBindgenTarget(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax @class)
        {
            return false;
        }

        var sourceCodeFilePath = @class.SyntaxTree.FilePath;
        if (sourceCodeFilePath.EndsWith(".g.cs", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
        }

        var isPartial = IsPartial(@class);
        if (!isPartial)
        {
            return false;
        }

        var hasBindgenAttribute = HasBindgenAttribute(@class);
        return hasBindgenAttribute;
    }

    private static bool IsPartial(MemberDeclarationSyntax member)
    {
        var isPartial = false;
        foreach (var modifier in member.Modifiers)
        {
            if (modifier.Text == "partial")
            {
                isPartial = true;
                break;
            }
        }

        return isPartial;
    }

    private static bool HasBindgenAttribute(ClassDeclarationSyntax @class)
    {
        var hasBindgenAttribute = false;
        foreach (var attributeList in @class.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToFullString();
                if (attributeName is not ("Bindgen" or "BindgenAttribute"))
                {
                    continue;
                }

                hasBindgenAttribute = true;
                break;
            }
        }

        return hasBindgenAttribute;
    }

    private static BindgenTarget BindgenTarget(GeneratorSyntaxContext context)
    {
        var @class = (ClassDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(@class)!;

        var sourceCodeFilePath = @class.SyntaxTree.FilePath;
        var fullName = symbol.ToDisplayString();
        var fullNameLastDotIndex = fullName.LastIndexOf('.');
        var namespaceName = fullName.Substring(0, fullNameLastDotIndex);
        var className = @class.Identifier.ValueText;
        var attributes = symbol.GetAttributes();
        var bindgenTarget = BindgenTarget(
            className, namespaceName, sourceCodeFilePath, attributes);

        return bindgenTarget;
    }

    private static void ExecuteTargets(SourceProductionContext context, ImmutableArray<BindgenTarget> targets)
    {
        if (targets.IsDefaultOrEmpty)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BindgenClassNotFound, Location.None));
            return;
        }

        foreach (var target in targets)
        {
            var inputCFilePath = target.Configuration.ReadCCode!.InputFilePath;
            if (!File.Exists(inputCFilePath))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BindgenHeaderNotFound, Location.None, inputCFilePath));
                continue;
            }

            ExecuteTarget(context, target);
        }
    }

    private static void ExecuteTarget(
        SourceProductionContext context,
        BindgenTarget target)
    {
        var className = target.Configuration.WriteCSharpCode!.ClassName;
        WriteConfiguration(target.OutputConfigurationFilePath, target.Configuration);

        try
        {
            Bindgen(target.OutputConfigurationFilePath);
        }
#pragma warning disable CA1031
        catch
#pragma warning restore CA1031
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.BindgenFailed,
                Location.None,
                className,
                target.OutputConfigurationFilePath,
                target.OutputLogFilePath));
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
                target.OutputConfigurationFilePath,
                target.OutputLogFilePath));
            return;
        }

        if (target.AddAsSource)
        {
            var sourceCode = File.ReadAllText(sourceCodeFileInfo.FullName);
            var fileName = Path.GetFileName(sourceCodeFileInfo.FullName);
            context.AddSource(fileName, SourceText.From(sourceCode, Encoding.UTF8));
            sourceCodeFileInfo.Delete();
        }
    }

    private static void Bindgen(string configurationFilePath)
    {
        var args = new[] { $"-c {configurationFilePath}"};
        using var host = Startup.CreateHost(args);
        host.Run();
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

    private static string GetWorkingDirectory(string sourceCodeFilePath, string? workingDirectoryCandidate)
    {
        string result;

        if (string.IsNullOrEmpty(workingDirectoryCandidate))
        {
            result = Path.GetDirectoryName(sourceCodeFilePath) ?? string.Empty;
        }
        else
        {
            result = workingDirectoryCandidate!;
        }

        var info = Directory.CreateDirectory(result);
        return info.Exists ? info.FullName : string.Empty;
    }

    private static string GetHeaderInputFilePath(string workingDirectory, string file)
    {
        var filePath = file;
        if (!File.Exists(filePath))
        {
            filePath = Path.Combine(workingDirectory, file);
        }

        return filePath;
    }

    private static string GetBindgenExecutableFilePath(string workingDirectory, string? bindgenExecutableFilePath)
    {
        if (!string.IsNullOrEmpty(bindgenExecutableFilePath))
        {
            var executableFilePath = !Path.IsPathRooted(bindgenExecutableFilePath) ? Path.GetFullPath(Path.Combine(workingDirectory, bindgenExecutableFilePath)) : bindgenExecutableFilePath!;
            return executableFilePath;
        }

        Terminal.ShellOutput shellOutput;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            shellOutput = "Show-Command -Name \"c2cs\"".ExecuteShell();
        }
        else
        {
            shellOutput = "command -v c2cs".ExecuteShell();
        }

        return shellOutput.ExitCode != 0 ? string.Empty : shellOutput.Output;
    }

    private static string GetOutputDirectory(string workingDirectory, string outputDirectory)
    {
        if (string.IsNullOrEmpty(outputDirectory))
        {
            return workingDirectory;
        }

        var result = !Path.IsPathRooted(outputDirectory) ? Path.GetFullPath(Path.Combine(workingDirectory, outputDirectory)) : outputDirectory;

        // Create directory if it does not already exist
        Directory.CreateDirectory(result);

        return result;
    }

    private static string GetConfigurationFilePath(
        string outputDirectory,
        string className)
    {
        return Path.Combine(outputDirectory, $"{className}.json");
    }

    private static string GetOutputLogFilePath(
        string workingDirectory,
        string className)
    {
        return Path.Combine(workingDirectory, $"{className}.log");
    }

    private static string GetOutputAbstractSyntaxTreesDirectory(string workingDirectory)
    {
        return Path.Combine(workingDirectory, "ast");
    }

    private static BindgenTarget BindgenTarget(
        string className,
        string namespaceName,
        string sourceCodeFilePath,
        ImmutableArray<AttributeData> attributes)
    {
        var bindgenAttributes = BindgenAttributes(attributes);
        var workingDirectory = GetWorkingDirectory(sourceCodeFilePath, bindgenAttributes.Bindgen.WorkingDirectory);
        var outputDirectory = GetOutputDirectory(workingDirectory, bindgenAttributes.Bindgen.OutputDirectory);
        var outputConfigurationFilePath = GetConfigurationFilePath(outputDirectory, className);
        var outputLogFilePath = GetOutputLogFilePath(outputDirectory, className);
        var configuration = Configuration(
            workingDirectory,
            outputDirectory,
            className,
            namespaceName,
            sourceCodeFilePath,
            bindgenAttributes);

        var target = new BindgenTarget
        {
            WorkingDirectory = workingDirectory,
            OutputConfigurationFilePath = outputConfigurationFilePath,
            OutputLogFilePath = outputLogFilePath,
            Configuration = configuration,
            AddAsSource = bindgenAttributes.Bindgen.IsEnabledAddAsSource
        };

        return target;
    }

    private static BindgenAttributes BindgenAttributes(ImmutableArray<AttributeData> attributes)
    {
        BindgenAttribute bindgenAttribute = null!;
        var targetPlatformAttributes = ImmutableArray.CreateBuilder<BindgenTargetPlatformAttribute>();
        var functionAttributes = ImmutableArray.CreateBuilder<BindgenFunctionAttribute>();

        foreach (var attribute in attributes)
        {
            var attributeName = attribute.AttributeClass!.Name;
            switch (attributeName)
            {
                case nameof(BindgenAttribute):
                    bindgenAttribute = CreateAttribute<BindgenAttribute>(attribute);
                    break;
                case nameof(BindgenTargetPlatformAttribute):
                    var targetPlatformAttribute = CreateAttribute<BindgenTargetPlatformAttribute>(attribute);
                    targetPlatformAttributes.Add(targetPlatformAttribute);
                    break;
                case nameof(BindgenFunctionAttribute):
                {
                    var functionAttribute = CreateAttribute<BindgenFunctionAttribute>(attribute);
                    functionAttributes.Add(functionAttribute);
                    break;
                }
            }
        }

        var result = new BindgenAttributes
        {
            Bindgen = bindgenAttribute,
            TargetPlatforms = targetPlatformAttributes.ToImmutableArray(),
            Functions = functionAttributes.ToImmutableArray()
        };

        return result;
    }

    private static BindgenConfiguration Configuration(
        string workingDirectory,
        string outputDirectory,
        string className,
        string namespaceName,
        string sourceCodeFilePath,
        BindgenAttributes attributes)
    {
        var inputCFilePath = GetHeaderInputFilePath(workingDirectory, attributes.Bindgen.HeaderInputFile);
        var outputAbstractSyntaxTreesDirectory = GetOutputAbstractSyntaxTreesDirectory(outputDirectory);
        var outputFileName = Path.GetFileNameWithoutExtension(sourceCodeFilePath) + ".g.cs";
        var outputCSharpFilePath = Path.Combine(outputDirectory, outputFileName);
        var configurationPlatforms = new Dictionary<string, ReadCodeCConfigurationPlatform?>();

        var readCodeC = new ReadCodeCConfiguration
        {
            WorkingDirectory = workingDirectory,
            InputFilePath = inputCFilePath,
            IsEnabledSystemDeclarations = attributes.Bindgen.IsEnabledSystemDeclarations,
            ConfigurationPlatforms = configurationPlatforms
        };

        var writeCodeCSharp = new WriteCodeCSharpConfiguration
        {
            WorkingDirectory = workingDirectory,
            OutputFilePath = outputCSharpFilePath,
            ClassName = className,
            NamespaceName = namespaceName,
            LibraryName = attributes.Bindgen.LibraryName
        };

        var configuration = new BindgenConfiguration
        {
            InputOutputFileDirectory = outputAbstractSyntaxTreesDirectory,
            ReadCCode = readCodeC,
            WriteCSharpCode = writeCodeCSharp
        };

        foreach (var attribute in attributes.TargetPlatforms)
        {
            var exists = configurationPlatforms.TryGetValue(attribute.Name, out var configurationPlatform);
            if (!exists)
            {
                configurationPlatform = new ReadCodeCConfigurationPlatform();
                configurationPlatforms.Add(attribute.Name, configurationPlatform);
            }

            configurationPlatform!.Frameworks = attribute.Frameworks.Cast<string?>().ToImmutableArray();
        }

        var functionNamesAllowed = ImmutableArray.CreateBuilder<string?>();
        foreach (var attribute in attributes.Functions)
        {
            functionNamesAllowed.Add(attribute.Name);
        }

        readCodeC.FunctionNamesAllowed = functionNamesAllowed.ToImmutable();

        return configuration;
    }

    private static T CreateAttribute<T>(AttributeData attribute)
        where T : Attribute
    {
        T result;
        if (attribute.ConstructorArguments.IsDefaultOrEmpty)
        {
            result = Activator.CreateInstance<T>();
        }
        else
        {
            var argumentValues = attribute.ConstructorArguments.Select(x => x.Value);
            result = (T)Activator.CreateInstance(typeof(T), argumentValues);
        }

        if (attribute.NamedArguments.IsDefaultOrEmpty)
        {
            return result;
        }

        var type = result.GetType();
        foreach (var argument in attribute.NamedArguments)
        {
            var propertyName = argument.Key;
            var property = type.GetRuntimeProperty(propertyName);
            if (property == null)
            {
                continue;
            }

            var propertyValue = TypedConstantValue(argument.Value);
            property.SetValue(result, propertyValue);
        }

        return result;
    }

    private static object TypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.Kind != TypedConstantKind.Array)
        {
            return typedConstant.Value!;
        }

        var elementType = typedConstant.Values[0].Value!.GetType();
        var array = Array.CreateInstance(elementType, typedConstant.Values.Length);
        for (var i = 0; i < typedConstant.Values.Length; i++)
        {
            var value = TypedConstantValue(typedConstant.Values[i]);
            array.SetValue(value, i);
        }

        return array;
    }
}
