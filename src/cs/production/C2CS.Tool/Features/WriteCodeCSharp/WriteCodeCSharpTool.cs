// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.IO.Abstractions;
using C2CS.Features.WriteCodeCSharp.Data;
using C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.Features.WriteCodeCSharp.Domain.Mapper;
using C2CS.Features.WriteCodeCSharp.Input;
using C2CS.Features.WriteCodeCSharp.Input.Sanitized;
using C2CS.Features.WriteCodeCSharp.Input.Unsanitized;
using C2CS.Features.WriteCodeCSharp.Output;
using C2CS.Foundation.Tool;
using CAstFfi.Data;
using CAstFfi.Data.Serialization;
using Microsoft.Extensions.Logging;

namespace C2CS.Features.WriteCodeCSharp;

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

        var project = output.Project = GenerateCSharpLibrary(nodesPerPlatform, input.GeneratorOptions);
        if (project == null)
        {
            return;
        }

        WriteFilesToStorage(input.OutputFileDirectory, project);

        if (input.GeneratorOptions.IsEnabledVerifyCSharpCodeCompiles)
        {
            output.CompilerResult = VerifyCSharpCodeCompiles(project, input.GeneratorOptions);
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

    private CSharpProject? GenerateCSharpLibrary(
        CSharpAbstractSyntaxTree abstractSyntaxTree,
        CSharpCodeGeneratorOptions options)
    {
        BeginStep("Generate C# library files");

        var codeGenerator = new CSharpCodeGenerator(_services, options);
        var project = codeGenerator.Generate(abstractSyntaxTree, Diagnostics);

        EndStep();

        return project;
    }

    private void WriteFilesToStorage(
        string outputFileDirectory,
        CSharpProject project)
    {
        BeginStep("Write generated files to storage");

        foreach (var document in project.Documents)
        {
            var fullFilePath = Path.GetFullPath(Path.Combine(outputFileDirectory, document.FileName));
            File.WriteAllText(fullFilePath, document.Contents);
        }

        EndStep();
    }

    private CSharpLibraryCompilerResult? VerifyCSharpCodeCompiles(
        CSharpProject project,
        CSharpCodeGeneratorOptions options)
    {
        BeginStep("Verify C# code compiles");

        var compiler = new CSharpLibraryCompiler();
        var result = compiler.Compile(project, options, Diagnostics);

        EndStep();

        return result;
    }
}
