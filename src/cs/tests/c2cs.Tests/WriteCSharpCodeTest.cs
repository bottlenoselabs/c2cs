// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using bottlenoselabs.Common;
using C2CS.GenerateCSharpCode;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;

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
        GenerateCrossPlatformFfi(fullDirectoryPath);
        var cSharpConfigFilePath = _fileSystem.Path.Combine(fullDirectoryPath, "config-generate-cs.json");
        var output = _tool.Run(cSharpConfigFilePath);
        Assert.True(output.IsSuccess);
        var ast = CreateCSharpAbstractSyntaxTree(output);
        return ast;
    }

    private void GenerateCrossPlatformFfi(string fullDirectoryPath)
    {
        var extractConfigFilePath = _fileSystem.Path.Combine(fullDirectoryPath, "config-extract.json");
        var extractShellOutput = $"c2ffi extract --config {extractConfigFilePath}".ExecuteShellCommand();
        Assert.True(extractShellOutput.ExitCode == 0, $"error extracting platform FFIs: \n{extractShellOutput.Output}");
        var ffiDirectoryPath = _fileSystem.Path.Combine(fullDirectoryPath, "ffi");
        var crossFfiFilePath = _fileSystem.Path.Combine(fullDirectoryPath, "ffi-x", "cross-platform.json");
        var ffiShellOutput =
            $"c2ffi merge --inputDirectoryPath {ffiDirectoryPath} --outputFilePath {crossFfiFilePath}"
                .ExecuteShellCommand();
        Assert.True(ffiShellOutput.ExitCode == 0, $"error merging platform FFIs:\n{ffiShellOutput.Output}");
    }

    private CSharpTestAbstractSyntaxTree CreateCSharpAbstractSyntaxTree(Output output)
    {
        var codeFilePath =
            _fileSystem.Path.Combine(output.OutputFileDirectory, $"{output.Input.ClassName}.g.cs");
        var code = _fileSystem.File.ReadAllText(codeFilePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var compilationUnitSyntax = syntaxTree.GetCompilationUnitRoot();
        var input = output.Input;

        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(string).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        Assert.True(compilationUnitSyntax.Members.Count == 1);
        var @namespace = compilationUnitSyntax.Members[0] as BaseNamespaceDeclarationSyntax;
        Assert.True(@namespace != null);
        Assert.True(@namespace.Name.ToString() == input.NamespaceName);

        Assert.True(@namespace.Members.Count == 1);
        var @class = @namespace.Members[0] as ClassDeclarationSyntax;
        Assert.True(@class != null);
        Assert.True(@class.Identifier.ToString() == input.ClassName);

        var functionsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestFunction>();
        var enumsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestEnum>();
        var structsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestStruct>();
        var macroObjectsByNameBuilder = ImmutableDictionary.CreateBuilder<string, CSharpTestMacroObject>();

        foreach (var member in @class.Members)
        {
            CreateTestNode(
                semanticModel,
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
        SemanticModel semanticModel,
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
                var value = new CSharpTestFunction(semanticModel, syntaxNode);
                methodsByNameBuilder.Add(syntaxNode.Identifier.Text, value);
                break;
            }

            case EnumDeclarationSyntax syntaxNode:
            {
                var value = new CSharpTestEnum(semanticModel, syntaxNode);
                enumsByNameBuilder.Add(syntaxNode.Identifier.Text, value);
                break;
            }

            case StructDeclarationSyntax syntaxNode:
            {
                var value = new CSharpTestStruct(syntaxNode);
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

                var value = new CSharpTestMacroObject(syntaxNode, fieldName);
                macroObjectsByNameBuilder.Add(fieldName, value);
                break;
            }

            default:
                throw new NotImplementedException();
        }
    }
}
