// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Data.C.Model;
using C2CS.Data.C.Serialization;
using C2CS.Data.CSharp.Model;
using C2CS.Foundation.Executors;
using C2CS.Options;
using C2CS.WriteCodeCSharp.Data;
using C2CS.WriteCodeCSharp.Data.Models;
using C2CS.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.WriteCodeCSharp.Domain.CodeGenerator.Diagnostics;
using C2CS.WriteCodeCSharp.Domain.Mapper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace C2CS.WriteCodeCSharp;

public sealed class FeatureWriteCodeCSharp : Executor<WriterCSharpCodeOptions, WriteCodeCSharpInput, WriteCodeCSharpOutput>
{
    private readonly CJsonSerializer _serializer;
    private readonly IServiceProvider _services;

    public FeatureWriteCodeCSharp(
        ILogger<FeatureWriteCodeCSharp> logger,
        WriteCodeCSharpInputValidator validator,
        CJsonSerializer serializer,
        IServiceProvider services)
        : base(logger, validator)
    {
        _serializer = serializer;
        _services = services;
    }

    protected override void Execute(WriteCodeCSharpInput input, WriteCodeCSharpOutput output)
    {
        var abstractSyntaxTreesC = LoadCAbstractSyntaxTrees(input.InputFilePaths);

        var nodesPerPlatform = MapCNodesToCSharpNodes(
            abstractSyntaxTreesC,
            input.MapperOptions);

        var code = GenerateCSharpCode(nodesPerPlatform, input.GeneratorOptions);
        WriteCSharpCodeToFileStorage(input.OutputFilePath, code);

        if (input.GeneratorOptions.IsEnabledVerifyCSharpCodeCompiles)
        {
            VerifyCSharpCodeCompiles(input.OutputFilePath, code);
        }
    }

    private ImmutableArray<CAbstractSyntaxTree> LoadCAbstractSyntaxTrees(ImmutableArray<string> filePaths)
    {
        BeginStep("Load C abstract syntax trees");

        var builder = ImmutableArray.CreateBuilder<CAbstractSyntaxTree>();
        foreach (var filePath in filePaths)
        {
            var ast = _serializer.Read(filePath);
            builder.Add(ast);
        }

        EndStep();

        return builder.ToImmutable();
    }

    private CSharpAbstractSyntaxTree MapCNodesToCSharpNodes(
        ImmutableArray<CAbstractSyntaxTree> abstractSyntaxTrees,
        CSharpCodeMapperOptions options)
    {
        BeginStep("Map C syntax tree nodes to C#");

        var mapper = new CSharpCodeMapper(options);
        var result = mapper.Map(Diagnostics, abstractSyntaxTrees);

        EndStep();

        return result;
    }

    private string GenerateCSharpCode(
        CSharpAbstractSyntaxTree abstractSyntaxTree,
        CSharpCodeGeneratorOptions options)
    {
        BeginStep("Generate C# code");

        var codeGenerator = new CSharpCodeGenerator(_services, options);
        var result = codeGenerator.EmitCode(abstractSyntaxTree);

        EndStep();

        return result;
    }

    private void VerifyCSharpCodeCompiles(string outputFilePath, string codeCSharp)
    {
        BeginStep("Verify C# code compiles");

        var syntaxTree = CSharpSyntaxTree.ParseText(codeCSharp);
        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithPlatform(Platform.AnyCpu)
            .WithAllowUnsafe(true);
        var compilation = CSharpCompilation.Create(
            "TestAssemblyName",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            compilationOptions);
        using var dllStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var emitResult = compilation.Emit(dllStream, pdbStream);

        foreach (var diagnostic in emitResult.Diagnostics)
        {
            // Obviously errors should be considered, but should warnings be considered too? Yes, yes they should. Some warnings can be indicative of bindings which are not correct.
            var isErrorOrWarning = diagnostic.Severity is
                Microsoft.CodeAnalysis.DiagnosticSeverity.Error or Microsoft.CodeAnalysis.DiagnosticSeverity.Warning;
            if (!isErrorOrWarning)
            {
                continue;
            }

            Diagnostics.Add(new CSharpCompileDiagnostic(outputFilePath, diagnostic));
        }

        EndStep();
    }

    private void WriteCSharpCodeToFileStorage(
        string outputFilePath, string codeCSharp)
    {
        BeginStep("Write C# code to file");

        File.WriteAllText(outputFilePath, codeCSharp);

        EndStep();
    }
}
