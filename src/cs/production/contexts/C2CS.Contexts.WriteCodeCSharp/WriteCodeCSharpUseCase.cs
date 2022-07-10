// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;
using C2CS.Contexts.ReadCodeC.Data.Serialization;
using C2CS.Contexts.WriteCodeCSharp.Data;
using C2CS.Contexts.WriteCodeCSharp.Data.Model;
using C2CS.Contexts.WriteCodeCSharp.Domain;
using C2CS.Contexts.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.Contexts.WriteCodeCSharp.Domain.CodeGenerator.Diagnostics;
using C2CS.Contexts.WriteCodeCSharp.Domain.Mapper;
using C2CS.Foundation.UseCases;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using DiagnosticSeverity = Microsoft.CodeAnalysis.DiagnosticSeverity;

namespace C2CS.Contexts.WriteCodeCSharp;

public sealed class WriteCodeCSharpUseCase : UseCase<WriteCodeCSharpConfiguration, WriteCodeCSharpInput, WriteCodeCSharpOutput>
{
    private readonly CJsonSerializer _serializer;

    public WriteCodeCSharpUseCase(
        ILogger<WriteCodeCSharpUseCase> logger, WriteCodeCSharpValidator validator, CJsonSerializer serializer)
        : base(logger, validator)
    {
        _serializer = serializer;
    }

    protected override void Execute(WriteCodeCSharpInput input, WriteCodeCSharpOutput output)
    {
        var abstractSyntaxTreesC = LoadCAbstractSyntaxTrees(input.InputFilePaths);

        var nodesPerPlatform = MapCNodesToCSharpNodes(
            abstractSyntaxTreesC,
            input.MapperOptions);

        var code = GenerateCSharpCode(nodesPerPlatform, input.GeneratorOptions);

        VerifyCSharpCodeCompiles(code);

        WriteCSharpCodeToFileStorage(input.OutputFilePath, code);
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

        var codeGenerator = new CSharpCodeGenerator(options);
        var result = codeGenerator.EmitCode(abstractSyntaxTree);

        EndStep();

        return result;
    }

    private void VerifyCSharpCodeCompiles(string codeCSharp)
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
            var isErrorOrWarning = diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning;
            if (!isErrorOrWarning)
            {
                continue;
            }

            Diagnostics.Add(new CSharpCompileDiagnostic(diagnostic));
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
