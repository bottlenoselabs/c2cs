// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.IO.Abstractions;
using bottlenoselabs.Common.Tools;
using c2ffi.Data;
using c2ffi.Data.Serialization;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

public sealed class Tool(
    ILogger<Tool> logger,
    InputSanitizer inputSanitizer,
    IServiceProvider services,
    IFileSystem fileSystem) : Tool<InputUnsanitized, InputSanitized, Output>(logger, inputSanitizer, fileSystem)
{
    private readonly IFileSystem _fileSystem = fileSystem;

    public new Output Run(string configurationFilePath)
    {
        return base.Run(configurationFilePath);
    }

    protected override void Execute(InputSanitized input, Output output)
    {
        var ffi = LoadCFfi(input.InputFilePath);
        var project = output.Project = GenerateCSharpProjectLibrary(ffi, input);
        WriteFiles(input.OutputFileDirectory, project);
    }

    private CFfiCrossPlatform LoadCFfi(string filePath)
    {
        BeginStep("Load C cross-platform FFI");

        var ffi = Json.ReadFfiCrossPlatform(_fileSystem, filePath);

        EndStep();

        return ffi;
    }

    private CodeProject GenerateCSharpProjectLibrary(
        CFfiCrossPlatform ffi,
        InputSanitized input)
    {
        BeginStep("Generate C# code");

        var codeGenerator = new CodeGenerator(services, input);
        var project = codeGenerator.GenerateCodeProject(ffi, Diagnostics);

        EndStep();

        return project;
    }

    private void WriteFiles(
        string outputFileDirectory,
        CodeProject project)
    {
        if (project.Documents.IsEmpty)
        {
            return;
        }

        BeginStep("Write generated files");

        foreach (var document in project.Documents)
        {
            var fullFilePath = Path.GetFullPath(Path.Combine(outputFileDirectory, document.FileName));
            File.WriteAllText(fullFilePath, document.Code);
        }

        EndStep();
    }
}
