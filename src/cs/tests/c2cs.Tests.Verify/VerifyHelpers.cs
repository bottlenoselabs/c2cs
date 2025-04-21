// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using C2CS.GenerateCSharpCode;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace c2cs.Tests.Verify;

public static class VerifyHelpers
{
    private const string C2CsVersionScrubberStart = "//      https://github.com/bottlenoselabs/c2cs (";
    private const string C2CsVersionScrubberReplace = "//      https://github.com/bottlenoselabs/c2cs (Version)";

    public static async Task VerifyOutput(
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
                    .Verify(codeWithHintName, extension: "cs")
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
}
