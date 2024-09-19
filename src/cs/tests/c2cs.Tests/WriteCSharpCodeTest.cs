// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using bottlenoselabs.Common;
using C2CS.BuildCLibrary;
using C2CS.Tests.Helpers;
using C2CS.Tests.Models;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using InputSanitized = C2CS.BuildCLibrary.InputSanitized;
using Output = C2CS.WriteCodeCSharp.Output;
using Tool = C2CS.WriteCodeCSharp.Tool;

namespace C2CS.Tests;

[PublicAPI]
[ExcludeFromCodeCoverage]
public abstract class WriteCSharpCodeTest
{
    private readonly IFileSystem _fileSystem;
    private readonly FileSystemHelper _fileSystemHelper;
    private readonly Tool _tool;

    protected WriteCSharpCodeTest()
    {
        var services = TestHost.Services;
        _fileSystem = services.GetService<IFileSystem>()!;
        _fileSystemHelper = services.GetService<FileSystemHelper>()!;
        _tool = services.GetService<Tool>()!;
    }

    public CSharpTestAbstractSyntaxTree GetCSharpAbstractSyntaxTree(string directoryPath)
    {
        var fullDirectoryPath = _fileSystemHelper.GetFullDirectoryPath(directoryPath);
        BuildCLibrary(fullDirectoryPath);
        GenerateCrossPlatformFfi(fullDirectoryPath);
        var cSharpConfigFilePath = _fileSystem.Path.Combine(fullDirectoryPath, "config-generate-cs.json");
        var output = _tool.Run(cSharpConfigFilePath);
        Assert.True(output.IsSuccess);
        var ast = CreateCSharpAbstractSyntaxTree(output);
        return ast;
    }

    private static void BuildCLibrary(
        string cMakeDirectoryPath, ImmutableArray<string>? additionalCMakeArguments = null)
    {
        var services = TestHost.Services;
        var cmakeLibraryBuilder = services.GetService<CMakeLibraryBuilder>()!;

        var input = new InputSanitized
        {
            CMakeDirectoryPath = cMakeDirectoryPath,
            OutputDirectoryPath = AppContext.BaseDirectory
        };
        var result = cmakeLibraryBuilder.BuildLibrary(input, additionalCMakeArguments ?? ImmutableArray<string>.Empty);
        Assert.True(result, "Failed to build C library.");
    }

    private void GenerateCrossPlatformFfi(string fullDirectoryPath)
    {
        var extractConfigFilePath = _fileSystem.Path.Combine(fullDirectoryPath, "config-extract.json");
        var extractShellOutput = $"c2ffi extract --config {extractConfigFilePath}".ExecuteShellCommand();
        Assert.True(extractShellOutput.ExitCode == 0, "error extracting platform FFIs");
        var ffiDirectoryPath = _fileSystem.Path.Combine(fullDirectoryPath, "ffi");
        var crossFfiFilePath = _fileSystem.Path.Combine(fullDirectoryPath, "ffi-x", "cross-platform.json");
        var ffiShellOutput =
            $"c2ffi merge --inputDirectoryPath {ffiDirectoryPath} --outputFilePath {crossFfiFilePath}"
                .ExecuteShellCommand();
        Assert.True(ffiShellOutput.ExitCode == 0, "error merging platform FFIs");
    }

    private CSharpTestAbstractSyntaxTree CreateCSharpAbstractSyntaxTree(
        Output output)
    {
        var codeFilePath =
            _fileSystem.Path.Combine(output.OutputFileDirectory, $"{output.Input.GeneratorOptions.ClassName}.g.cs");
        var code = _fileSystem.File.ReadAllText(codeFilePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilationUnitSyntax = syntaxTree.GetCompilationUnitRoot();
        var generatorOptions = output.Input.GeneratorOptions;

        Assert.True(compilationUnitSyntax.Members.Count == 1);
        var @namespace = compilationUnitSyntax.Members[0] as BaseNamespaceDeclarationSyntax;
        Assert.True(@namespace != null);
        Assert.True(@namespace!.Name.ToString() == generatorOptions.NamespaceName);

        Assert.True(@namespace.Members.Count == 1);
        var @class = @namespace.Members[0] as ClassDeclarationSyntax;
        Assert.True(@class != null);
        Assert.True(@class!.Identifier.ToString() == generatorOptions.ClassName);

        var functionsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestFunction>();
        var enumsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestEnum>();
        var structsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestStruct>();
        var macroObjectsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestMacroObject>();

        foreach (var member in @class.Members)
        {
            CreateTestNode(
                member,
                functionsByNameBuilder,
                enumsByNameBuilder,
                structsByNameBuilder,
                macroObjectsByNameBuilder);
        }

        var ast = new CSharpTestAbstractSyntaxTree(
            enumsByNameBuilder.ToImmutable(),
            functionsByNameBuilder.ToImmutable(),
            macroObjectsByNameBuilder.ToImmutable(),
            structsByNameBuilder.ToImmutable());
        return ast;
    }

    private void CreateTestNode(
        MemberDeclarationSyntax member,
        ImmutableDictionary<string, CSharpTestFunction>.Builder methodsByNameBuilder,
        ImmutableDictionary<string, CSharpTestEnum>.Builder enumsByNameBuilder,
        ImmutableDictionary<string, CSharpTestStruct>.Builder structsByNameBuilder,
        ImmutableDictionary<string, CSharpTestMacroObject>.Builder macroObjectsByNameBuilder)
    {
        switch (member)
        {
            case MethodDeclarationSyntax syntaxNode:
            {
                var value = CreateTestFunction(syntaxNode);
                methodsByNameBuilder.Add(syntaxNode.Identifier.Text, value);
                break;
            }

            case EnumDeclarationSyntax syntaxNode:
            {
                var value = CreateTestEnum(syntaxNode);
                enumsByNameBuilder.Add(syntaxNode.Identifier.Text, value);
                break;
            }

            case StructDeclarationSyntax syntaxNode:
            {
                var value = CreateTestStruct(syntaxNode);
                structsByNameBuilder.Add(syntaxNode.Identifier.Text, value);
                break;
            }

            case FieldDeclarationSyntax syntaxNode:
            {
                var fieldName = syntaxNode.Declaration.Variables[0].Identifier.Text;
                if (fieldName == "LibraryName")
                {
                    return;
                }

                var value = CreateTestMacroObject(syntaxNode, fieldName);
                macroObjectsByNameBuilder.Add(fieldName, value);
                break;
            }
        }
    }

    private CSharpTestFunction CreateTestFunction(MethodDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.Text;
        var returnTypeName = syntaxNode.ReturnType.ToString();
        var parameters = CreateTestFunctionParameters(syntaxNode);

        var result = new CSharpTestFunction
        {
            Name = name,
            ReturnTypeName = returnTypeName,
            Parameters = parameters
        };
        return result;
    }

    private ImmutableArray<CSharpTestFunctionParameter> CreateTestFunctionParameters(MethodDeclarationSyntax syntaxNode)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpTestFunctionParameter>();

        foreach (var syntaxNodeParameter in syntaxNode.ParameterList.Parameters)
        {
            var parameter = CreateTestFunctionParameter(syntaxNodeParameter);
            builder.Add(parameter);
        }

        return builder.ToImmutable();
    }

    private CSharpTestFunctionParameter CreateTestFunctionParameter(ParameterSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.ValueText;
        var typeName = syntaxNode.Type?.ToString() ?? string.Empty;

        var result = new CSharpTestFunctionParameter
        {
            Name = name,
            TypeName = typeName
        };
        return result;
    }

    private CSharpTestEnum CreateTestEnum(EnumDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.Text;
        var baseType = syntaxNode.BaseList!.Types[0].Type.ToString();
        var enumMembers = CreateTestEnumMembers(syntaxNode);

        var result = new CSharpTestEnum
        {
            Name = name,
            BaseType = baseType,
            Members = enumMembers
        };
        return result;
    }

    private ImmutableArray<CSharpTestEnumMember> CreateTestEnumMembers(EnumDeclarationSyntax syntaxNode)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpTestEnumMember>();
        foreach (var syntaxNodeEnumMember in syntaxNode.Members)
        {
            var enumMember = CreateTestEnumMember(syntaxNodeEnumMember);
            builder.Add(enumMember);
        }

        return builder.ToImmutable();
    }

    private CSharpTestEnumMember CreateTestEnumMember(EnumMemberDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.ValueText;
        var value = syntaxNode.EqualsValue!.Value.GetText().ToString().Trim();

        var result = new CSharpTestEnumMember
        {
            Name = name,
            Value = value
        };
        return result;
    }

    private CSharpTestStruct CreateTestStruct(StructDeclarationSyntax syntaxNode)
    {
        var name = syntaxNode.Identifier.Text;
        var layout = CreateTestStructLayout(syntaxNode);
        var fields = CreateTestStructFields(syntaxNode, layout);

        var result = new CSharpTestStruct
        {
            Name = name,
            Layout = layout,
            Fields = fields
        };
        return result;
    }

    private ImmutableArray<CSharpTestStructField> CreateTestStructFields(
        StructDeclarationSyntax syntaxNode,
        CSharpTestStructLayout layout)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpTestStructField>();

        foreach (var syntaxNodeMember in syntaxNode.Members)
        {
            if (syntaxNodeMember is not FieldDeclarationSyntax syntaxNodeField)
            {
                continue;
            }

            var field = CreateTestStructField(syntaxNodeField, layout);
            builder.Add(field);
        }

        return builder.ToImmutable();
    }

    private CSharpTestStructField CreateTestStructField(
        FieldDeclarationSyntax syntaxNode,
        CSharpTestStructLayout structLayout)
    {
        var variableSyntaxNode = syntaxNode.Declaration;
        var name = variableSyntaxNode.Variables[0].Identifier.Text;
        var typeName = variableSyntaxNode.Type.ToString();

        int? offsetOf;
        if (structLayout.LayoutKind == "LayoutKind.Explicit")
        {
            offsetOf = FieldOffsetOf(name, syntaxNode);
        }
        else
        {
            offsetOf = null;
        }

        var result = new CSharpTestStructField
        {
            Name = name,
            TypeName = typeName,
            OffsetOf = offsetOf
        };

        return result;
    }

    private CSharpTestStructLayout CreateTestStructLayout(StructDeclarationSyntax syntaxNode)
    {
        var attribute = GetAttribute("StructLayout", syntaxNode);
        var arguments = attribute.ArgumentList!.Arguments;

        int? sizeOf;
        int? packOf;

        var layoutKind = arguments[0].Expression.ToFullString();
        if (layoutKind == "LayoutKind.Explicit")
        {
            var sizeOfString = arguments[1].Expression.ToFullString();
            sizeOf = int.Parse(sizeOfString, CultureInfo.InvariantCulture);
            var packOfString = arguments[2].Expression.ToFullString();
            packOf = int.Parse(packOfString, CultureInfo.InvariantCulture);
        }
        else
        {
            sizeOf = null;
            packOf = null;
        }

        var result = new CSharpTestStructLayout
        {
            LayoutKind = layoutKind,
            Size = sizeOf,
            Pack = packOf
        };
        return result;
    }

    private CSharpTestMacroObject CreateTestMacroObject(FieldDeclarationSyntax syntaxNode, string fieldName)
    {
        var typeName = syntaxNode.Declaration.Type.ToString();
        var value = syntaxNode.Declaration.Variables[0].Initializer!.Value.ToString();

        var result = new CSharpTestMacroObject
        {
            Name = fieldName,
            TypeName = typeName,
            Value = value
        };
        return result;
    }

    private int? FieldOffsetOf(string name, FieldDeclarationSyntax syntaxNode)
    {
        int? offsetOf = null;

        var attribute = GetAttribute("FieldOffset", syntaxNode);
        var expression = attribute.ArgumentList!.Arguments[0].Expression;
        if (expression is LiteralExpressionSyntax literalExpression)
        {
            offsetOf = int.Parse(literalExpression.Token.ValueText, CultureInfo.InvariantCulture);
        }

        Assert.True(offsetOf != null, $"The field `{name}` does not have an offset.");

        return offsetOf;
    }

    private AttributeSyntax GetAttribute(string name, MemberDeclarationSyntax syntaxNode)
    {
        AttributeSyntax? result = null;

        foreach (var attributeList in syntaxNode.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attributeName = attribute.Name.ToString();
                if (attributeName == name)
                {
                    result = attribute;
                }
            }
        }

        Assert.True(result != null, $"The attribute `{name}` does not exist.");

        return result!;
    }
}
