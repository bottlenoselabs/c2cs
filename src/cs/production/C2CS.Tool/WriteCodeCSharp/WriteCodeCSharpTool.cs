// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.IO.Abstractions;
using C2CS.Foundation.Tool;
using C2CS.WriteCodeCSharp.Data;
using C2CS.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.WriteCodeCSharp.Domain.CodeGenerator.Diagnostics;
using C2CS.WriteCodeCSharp.Domain.Mapper;
using C2CS.WriteCodeCSharp.Input;
using C2CS.WriteCodeCSharp.Input.Sanitized;
using C2CS.WriteCodeCSharp.Input.Unsanitized;
using C2CS.WriteCodeCSharp.Output;
using CAstFfi.Data;
using CAstFfi.Data.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace C2CS.WriteCodeCSharp;

public sealed class WriteCodeCSharpTool : Tool<WriteCSharpCodeOptions, WriteCodeCSharpInput, WriteCodeCSharpOutput>
{
    private readonly IServiceProvider _services;

    public WriteCodeCSharpTool(
        ILogger<WriteCodeCSharpTool> logger,
        WriteCodeCSharpInputSanitizer inputSanitizer,
        IServiceProvider services,
        IFileSystem fileSystem)
        : base(logger, inputSanitizer, fileSystem)
    {
        _services = services;
    }

    protected override void Execute(WriteCodeCSharpInput input, WriteCodeCSharpOutput output)
    {
        var abstractSyntaxTreesC = LoadCAbstractSyntaxTree(input.InputFilePath);

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

    private CAbstractSyntaxTreeCrossPlatform LoadCAbstractSyntaxTree(string filePath)
    {
        BeginStep("Load C abstract syntax tree");

        var ast = CJsonSerializer.ReadAbstractSyntaxTreeCrossPlatform(filePath);

        EndStep();

        return ast;
    }

    private CSharpAbstractSyntaxTree MapCNodesToCSharpNodes(
        CAbstractSyntaxTreeCrossPlatform abstractSyntaxTree,
        CSharpCodeMapperOptions options)
    {
        BeginStep("Map C syntax tree nodes to C#");

        var mapper = new CSharpCodeMapper(options);
        var result = mapper.Map(Diagnostics, abstractSyntaxTree);

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
