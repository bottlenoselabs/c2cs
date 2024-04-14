// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using bottlenoselabs.Common.Tools;
using C2CS.Commands.WriteCodeCSharp.Data;
using C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.Commands.WriteCodeCSharp.Domain.Mapper;
using C2CS.Commands.WriteCodeCSharp.Input;
using C2CS.Commands.WriteCodeCSharp.Input.Sanitized;
using C2CS.Commands.WriteCodeCSharp.Input.Unsanitized;
using C2CS.Commands.WriteCodeCSharp.Output;
using c2ffi.Data;
using c2ffi.Data.Serialization;
using Microsoft.Extensions.Logging;

namespace C2CS.Commands.WriteCodeCSharp;

public sealed class WriteCodeCSharpTool : Tool<WriteCSharpCodeOptions, WriteCodeCSharpInput, WriteCodeCSharpOutput>
{
    private readonly IServiceProvider _services;
    private readonly IFileSystem _fileSystem;

    public WriteCodeCSharpTool(
        ILogger<WriteCodeCSharpTool> logger,
        WriteCodeCSharpInputSanitizer inputSanitizer,
        IServiceProvider services,
        IFileSystem fileSystem)
        : base(logger, inputSanitizer, fileSystem)
    {
        _services = services;
        _fileSystem = fileSystem;
    }

    public new WriteCodeCSharpOutput Run(string configurationFilePath)
    {
        return base.Run(configurationFilePath);
    }

    protected override void Execute(WriteCodeCSharpInput input, WriteCodeCSharpOutput output)
    {
        var abstractSyntaxTreesC = LoadCFfi(input.InputFilePath);

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
            output.Assembly = VerifyCSharpCodeCompiles(project, input.GeneratorOptions);
        }
    }

    private CFfiCrossPlatform LoadCFfi(string filePath)
    {
        BeginStep("Load C cross-platform FFI");

        var ffi = Json.ReadFfiCrossPlatform(_fileSystem, filePath);

        EndStep();

        return ffi;
    }

    private CSharpAbstractSyntaxTree MapCNodesToCSharpNodes(
        CFfiCrossPlatform ffi,
        CSharpCodeMapperOptions options)
    {
        BeginStep("Map C syntax tree nodes to C#");

        var mapper = new CSharpCodeMapper(options);
        var result = mapper.Map(ffi, Diagnostics);

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

    private Assembly? VerifyCSharpCodeCompiles(
        CSharpProject project,
        CSharpCodeGeneratorOptions options)
    {
        BeginStep("Verify C# code compiles");

        var compiler = new CSharpLibraryCompiler();
        var assembly = compiler.Compile(project, options, Diagnostics);

        EndStep();

        return assembly;
    }
}
