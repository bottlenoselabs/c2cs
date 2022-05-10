// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS;

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
        var bindgenTargetData = BindgenTarget(attributes);
        if (bindgenTargetData == null)
        {
            return;
        }

        var bindgenAttribute = bindgenTargetData.Value.Item1;
        var bindgenAttributes = bindgenTargetData.Value.Item2;

        var workingDirectory = GetWorkingDirectory(sourceCodeFilePath, bindgenAttribute.WorkingDirectory);
        var headerInputFilePath = GetHeaderInputFilePath(workingDirectory, bindgenAttribute.HeaderInputFile);
        var configurationFilePath =
            GetConfigurationFilePath(workingDirectory, className, bindgenAttribute.ConfigurationFileName);
        var outputLogFilePath = GetOutputLogFilePath(workingDirectory, className, bindgenAttribute.OutputLogFileName);

        var configuration = new BindgenTargetConfiguration
        {
            InputFilePath = headerInputFilePath,
            ClassName = className,
            LibraryName = bindgenAttribute.LibraryName,
            NamespaceName = namespaceName,
            Attributes = bindgenAttributes,
            IsEnabledSystemDeclarations = bindgenAttribute.IsEnabledSystemDeclarations
        };

        var target = new BindgenTarget
        {
            WorkingDirectory = workingDirectory,
            ConfigurationFilePath = configurationFilePath,
            OutputLogFilePath = outputLogFilePath,
            CSharpInputFilePath = sourceCodeFilePath,
            Configuration = configuration,
            AddAsSource = bindgenAttribute.AddAsSource
        };

        Targets.Add(target);
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
        string className,
        string configurationFileName)
    {
        var fileName = !string.IsNullOrEmpty(configurationFileName) ?
            configurationFileName : $"{className}.json";
        return Path.Combine(workingDirectory, fileName);
    }

    private static string GetOutputLogFilePath(
        string workingDirectory,
        string className,
        string? outputLogFileName)
    {
        var fileName = !string.IsNullOrEmpty(outputLogFileName) ?
            outputLogFileName : $"{className}.log";
        return Path.Combine(workingDirectory, fileName);
    }

    private static (BindgenAttribute, BindgenTargetConfigurationAttributes)? BindgenTarget(
        ImmutableArray<AttributeData> attributes)
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

        var targetAttributes = new BindgenTargetConfigurationAttributes
        {
            TargetPlatforms = targetPlatformAttributes.ToImmutable(),
            Functions = functionAttributes.ToImmutable()
        };

        return (bindgenAttribute, targetAttributes);
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
