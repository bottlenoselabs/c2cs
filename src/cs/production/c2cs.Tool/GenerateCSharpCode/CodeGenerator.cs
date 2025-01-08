// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using bottlenoselabs.Common.Diagnostics;
using C2CS.GenerateCSharpCode.Generators;
using c2ffi.Data;
using c2ffi.Data.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

public sealed partial class CodeGenerator
{
    private readonly ILogger<CodeGenerator> _logger;

    private readonly CodeGeneratorDocumentPInvoke _codeGeneratorDocumentPInvoke;
    private readonly CodeGeneratorDocumentAssemblyAttributes _codeGeneratorDocumentAssemblyAttributes;
    private readonly CodeGeneratorDocumentInteropRuntime _codeGeneratorDocumentInteropRuntime;

    private readonly InputSanitized _input;
    private readonly ImmutableDictionary<Type, object> _nodeCodeGenerators;

    public CodeGenerator(
        IServiceProvider services,
        InputSanitized input)
    {
        _logger = services.GetRequiredService<ILogger<CodeGenerator>>();
        _input = input;
        _codeGeneratorDocumentPInvoke = services.GetRequiredService<CodeGeneratorDocumentPInvoke>();
        _codeGeneratorDocumentAssemblyAttributes = services.GetRequiredService<CodeGeneratorDocumentAssemblyAttributes>();
        _codeGeneratorDocumentInteropRuntime = services.GetRequiredService<CodeGeneratorDocumentInteropRuntime>();

        _nodeCodeGenerators = new Dictionary<Type, object>
        {
            { typeof(CEnum), services.GetRequiredService<GeneratorEnum>() },
            { typeof(CFunction), services.GetRequiredService<GeneratorFunction>() },
            { typeof(CFunctionPointer), services.GetRequiredService<GeneratorFunctionPointer>() },
            { typeof(CMacroObject), services.GetRequiredService<GeneratorMacroObject>() },
            { typeof(COpaqueType), services.GetRequiredService<GeneratorOpaqueType>() },
            { typeof(CRecord), services.GetRequiredService<GeneratorStruct>() },
            { typeof(CTypeAlias), services.GetRequiredService<GeneratorAliasType>() },
        }.ToImmutableDictionary();
    }

    public CodeProject GenerateCodeProject(CFfiCrossPlatform ffi, DiagnosticsSink diagnostics)
    {
        try
        {
            var options = new CodeGeneratorDocumentOptions(_input);
            var documents = ImmutableArray.CreateBuilder<CodeProjectDocument>();

            AddDocumentPInvoke(options, documents, ffi);
            AddDocumentInteropRuntime(options, documents);
            AddDocumentAssemblyAttributes(options, documents);

            var newDocuments = PostProcessDocuments(documents.ToImmutableArray());

            return new CodeProject
            {
                Documents = [..newDocuments]
            };
        }
#pragma warning disable CA1031
        catch (Exception e)
#pragma warning restore CA1031
        {
            diagnostics.Add(new DiagnosticPanic(e));
            return new CodeProject();
        }
    }

    private static ImmutableArray<CodeProjectDocument> PostProcessDocuments(
        ImmutableArray<CodeProjectDocument> documents)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.CurrentSolution.AddProject(
                "TemporaryProject", "TemporaryAssembly", LanguageNames.CSharp)
            .AddMetadataReferences([
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            ]);
        foreach (var document in documents)
        {
            project = project.AddDocument(document.FileName, document.Code).Project;
        }

        var newDocuments = new List<CodeProjectDocument>();
        var compilation = project.GetCompilationAsync().Result!;
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var code = PostProcessDocumentSyntaxTree(syntaxTree, compilation, project.Solution);
            var newDocument = new CodeProjectDocument
            {
                Code = code,
                FileName = syntaxTree.FilePath
            };
            newDocuments.Add(newDocument);
        }

        return [..newDocuments];
    }

    private static string PostProcessDocumentSyntaxTree(
        SyntaxTree syntaxTree,
        Compilation compilation,
        Solution solution)
    {
        var root = syntaxTree.GetRoot();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var functionPointerStructWrappers = root.DescendantNodes()
            .OfType<StructDeclarationSyntax>()
            .Where(x =>
                x.Identifier.Text.StartsWith("FnPtr_", StringComparison.InvariantCultureIgnoreCase))
            .ToImmutableArray();
        if (functionPointerStructWrappers.IsDefaultOrEmpty)
        {
            return root.SyntaxTree.ToString();
        }

        var newRoot = root;
        foreach (var functionPointerStructWrapper in functionPointerStructWrappers)
        {
            var symbol = semanticModel.GetDeclaredSymbol(functionPointerStructWrapper)!;
            var references = SymbolFinder
                .FindReferencesAsync(symbol, solution).Result
                .ToImmutableArray();

            var referencesCount = references.Sum(x => x.Locations.Count());
            if (referencesCount == 0)
            {
                newRoot = newRoot.RemoveNode(functionPointerStructWrapper, SyntaxRemoveOptions.KeepNoTrivia)!;
            }
        }

        return newRoot.SyntaxTree.ToString();
    }

    private void AddDocumentPInvoke(
        CodeGeneratorDocumentOptions options,
        ImmutableArray<CodeProjectDocument>.Builder documents,
        CFfiCrossPlatform ffi)
    {
        var context = new CodeGeneratorContext(_input, ffi, _nodeCodeGenerators);
        var document = _codeGeneratorDocumentPInvoke.Generate(options, context, ffi);
        documents.Add(document);

        var linesOfCodeCount = document.Code.Count(c => c.Equals('\n')) + 1;
        LogGeneratedCodeProjectDocument(document.FileName, linesOfCodeCount);
    }

    private void AddDocumentInteropRuntime(
        CodeGeneratorDocumentOptions options,
        ImmutableArray<CodeProjectDocument>.Builder documents)
    {
        if (!_input.IsEnabledGenerateCSharpRuntimeCode)
        {
            return;
        }

        var document = _codeGeneratorDocumentInteropRuntime.Generate(options);
        documents.Add(document);

        var linesOfCodeCount = document.Code.Count(c => c.Equals('\n')) + 1;
        LogGeneratedCodeProjectDocument(document.FileName, linesOfCodeCount);
    }

    private void AddDocumentAssemblyAttributes(
        CodeGeneratorDocumentOptions options,
        ImmutableArray<CodeProjectDocument>.Builder documents)
    {
        var document = _codeGeneratorDocumentAssemblyAttributes.Generate(options);
        documents.Add(document);

        var linesOfCodeCount = document.Code.Count(c => c.Equals('\n')) + 1;
        LogGeneratedCodeProjectDocument(document.FileName, linesOfCodeCount);
    }

    [LoggerMessage(0, LogLevel.Information, "- '{FileName}': {LinesOfCodeCount} lines of code")]
    private partial void LogGeneratedCodeProjectDocument(string fileName, int linesOfCodeCount);
}
