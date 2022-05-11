// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Reflection;
using C2CS.Contexts.WriteCodeCSharp.Data;
using C2CS.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.SourceGenerator;

// TODO:
//  2. Change AST directory; directory has be unique for the class name, even if it's going to be deleted after.
//  3. Remove the `header.h` file in macOS example; use CoreFoundation filepath directly.

public class BindgenSyntaxReceiver : ISyntaxContextReceiver
{
#pragma warning disable CA1002
    public List<BindgenTarget> Targets { get; } = new();
#pragma warning restore CA1002

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        var node = context.Node;
        if (node is not ClassDeclarationSyntax @class)
        {
            return;
        }

        var sourceCodeFilePath = @class.SyntaxTree.FilePath;
        if (sourceCodeFilePath.EndsWith(".g.cs", StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        var isPartial = IsPartial(@class);
        if (!isPartial)
        {
            return;
        }

        var fullName = context.SemanticModel.GetDeclaredSymbol(node)!.ToDisplayString();
        var fullNameLastDotIndex = fullName.LastIndexOf('.');
        var namespaceName = fullName.Substring(0, fullNameLastDotIndex);
        var className = @class.Identifier.ValueText;
        var symbol = context.SemanticModel.GetDeclaredSymbol(node);
        var attributes = symbol!.GetAttributes();
        var bindgenTarget = BindgenTarget(
            className, namespaceName, sourceCodeFilePath, attributes);
        if (bindgenTarget == null)
        {
            return;
        }

        Targets.Add(bindgenTarget);
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

    private static string GetConfigurationFilePath(
        string workingDirectory,
        string className)
    {
        return Path.Combine(workingDirectory, $"{className}.json");
    }

    private static string GetOutputLogFilePath(
        string workingDirectory,
        string className)
    {
        return Path.Combine(workingDirectory, $"{className}.log");
    }

    private static string GetOutputAbstractSyntaxTreeDirectory(string workingDirectory)
    {
        return Path.Combine(workingDirectory, "ast");
    }

    private static BindgenTarget? BindgenTarget(
        string className,
        string namespaceName,
        string sourceCodeFilePath,
        ImmutableArray<AttributeData> attributes)
    {
        var bindgenAttributes = BindgenAttributes(attributes);
        if (bindgenAttributes == null)
        {
            return null;
        }

        var workingDirectory = GetWorkingDirectory(sourceCodeFilePath, bindgenAttributes.Bindgen.WorkingDirectory);
        var outputConfigurationFilePath = GetConfigurationFilePath(workingDirectory, className);
        var outputLogFilePath = GetOutputLogFilePath(workingDirectory, className);
        var configuration = CreateConfiguration(
            workingDirectory, className, namespaceName, sourceCodeFilePath, bindgenAttributes);

        var target = new BindgenTarget
        {
            WorkingDirectory = workingDirectory,
            OutputConfigurationFilePath = outputConfigurationFilePath,
            OutputLogFilePath = outputLogFilePath,
            Configuration = configuration
        };

        return target;
    }

    private static BindgenAttributes? BindgenAttributes(ImmutableArray<AttributeData> attributes)
    {
        BindgenAttribute? bindgenAttribute = null;
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

        if (bindgenAttribute == null)
        {
            return null;
        }

        var result = new BindgenAttributes
        {
            Bindgen = bindgenAttribute,
            TargetPlatforms = targetPlatformAttributes.ToImmutableArray(),
            Functions = functionAttributes.ToImmutableArray()
        };

        return result;
    }

    private static BindgenConfiguration CreateConfiguration(
        string workingDirectory,
        string className,
        string namespaceName,
        string sourceCodeFilePath,
        BindgenAttributes attributes)
    {
        var inputCFilePath = GetHeaderInputFilePath(workingDirectory, attributes.Bindgen.HeaderInputFile);
        var outputAbstractSyntaxTreeDirectory = GetOutputAbstractSyntaxTreeDirectory(workingDirectory);
        var outputFileName = Path.GetFileNameWithoutExtension(sourceCodeFilePath) + ".g.cs";
        var sourceCodeDirectoryPath = Path.GetDirectoryName(sourceCodeFilePath)!;
        var outputCSharpFilePath = Path.Combine(sourceCodeDirectoryPath, outputFileName);
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
            InputOutputFileDirectory = outputAbstractSyntaxTreeDirectory,
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
