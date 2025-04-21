// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Runtime.CompilerServices;
using bottlenoselabs.Common;
using C2CS.GenerateCSharpCode;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace C2CS.Tests.Verify.Helpers;

public abstract class VerifyHelpers(FileSystemHelper fileSystemHelper, Tool tool)
{
    private const string C2CsVersionScrubberStart = "//      https://github.com/bottlenoselabs/c2cs (";
    private const string C2CsVersionScrubberReplace = "//      https://github.com/bottlenoselabs/c2cs (Version)";

    private readonly FileSystemHelper _fileSystemHelper = fileSystemHelper;
    private readonly Tool _tool = tool;

    protected async Task VerifyOutput(
        Output output,
        [CallerMemberName] string? callerMethodName = null,
        [CallerFilePath] string? callerFilePath = null)
    {
        output.Diagnostics.Should().BeEmpty();
        output.IsSuccess.Should().BeTrue();
        output.Project.Should().NotBeNull();
        output.OutputFileDirectory.Should().Be(output.Input.OutputFileDirectory);
        if (output.Project is null)
        {
            return;
        }

        // Ensure that the generated code matches the snapshots
        var codeFileName = Path.GetFileNameWithoutExtension(callerFilePath);
        var tasks = output.Project.Documents
            .Select(x => Task.Run(async () =>
            {
                var generatedFileName = Path.GetFileNameWithoutExtension(x.FileName);
                var codeWithHintName = $"// HintName: {x.FileName}\n{x.Code}";
                codeWithHintName = codeWithHintName.Replace("\r\n", "\n", StringComparison.InvariantCulture);
                await Verifier
                    // ReSharper disable once ExplicitCallerInfoArgument
                    .Verify(codeWithHintName, extension: "cs", sourceFile: callerFilePath ?? string.Empty)
                    .UseDirectory($"Snapshots/{codeFileName}/{callerMethodName}")
                    .UseFileName(generatedFileName)
                    .ScrubInlineDateTimeOffsets("yyyy-MM-dd HH:mm:ss 'GMT'zzz")
                    .ScrubLinesWithReplace(line =>
                        line.StartsWith(C2CsVersionScrubberStart, StringComparison.InvariantCulture)
                            ? C2CsVersionScrubberReplace
                            : line);
            }));
        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Ensure the generated code actually compiles
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees: output.Project.Documents.Select(x => CSharpSyntaxTree.ParseText(x.Code)),
            references: [MetadataReference.CreateFromFile(typeof(string).Assembly.Location)],
            options: new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        compilation
            .GetDiagnostics()
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .Should().BeEmpty();
    }

    protected Output RunTool(string relativeConfigPath)
    {
        var fullPath = _fileSystemHelper.GetFullDirectoryPath("src/cs/tests/c2cs.Tests.Verify/configs");
        return _tool.Run($"{fullPath}/{relativeConfigPath}");
    }

    protected Output RunTool(string relativeSourcePath, string relativeConfigPath)
    {
        GenerateCrossPlatformFfi($"src/c/tests/{relativeSourcePath}");

        return RunTool(relativeConfigPath);
    }

    private void GenerateCrossPlatformFfi(string relativeDirectoryPath)
    {
        var fullDirectoryPath = _fileSystemHelper.GetFullDirectoryPath(relativeDirectoryPath);
        var fileSystem = _fileSystemHelper.FileSystem;
        var extractConfigFilePath = fileSystem.Path.Combine(fullDirectoryPath, "config-extract.json");
        var extractShellOutput = $"c2ffi extract --config {extractConfigFilePath}".ExecuteShellCommand();
        Assert.True(extractShellOutput.ExitCode == 0, $"error extracting platform FFIs: \n{extractShellOutput.Output}");
        var ffiDirectoryPath = fileSystem.Path.Combine(fullDirectoryPath, "ffi");
        var crossFfiFilePath = fileSystem.Path.Combine(fullDirectoryPath, "ffi-x", "cross-platform.json");
        var ffiShellOutput =
            $"c2ffi merge --inputDirectoryPath {ffiDirectoryPath} --outputFilePath {crossFfiFilePath}"
                .ExecuteShellCommand();
        Assert.True(ffiShellOutput.ExitCode == 0, $"error merging platform FFIs:\n{ffiShellOutput.Output}");
    }
}
