// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Runtime.CompilerServices;
using C2CS.GenerateCSharpCode;
using FluentAssertions;

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

        var codeFileName = Path.GetFileNameWithoutExtension(callerFilePath);
        var tasks = output.Project.Documents
            .Select(x => Task.Run(async () =>
            {
                var generatedFileName = Path.GetFileNameWithoutExtension(x.FileName);
                var codeWithHintName = $"// HintName: {x.FileName}\n{x.Code}";
                await Verifier
                    .Verify(codeWithHintName, extension: "cs")
                    .UseDirectory($"Snapshots/{codeFileName}/{callerMethodName}")
                    .UseFileName(generatedFileName)
                    .ScrubInlineDateTimeOffsets("yyyy-MM-dd HH:mm:ss 'GMT'zzz")
                    .ScrubLinesWithReplace(line =>
                        line.StartsWith(C2CsVersionScrubberStart, StringComparison.InvariantCulture)
                            ? C2CsVersionScrubberReplace
                            : line);
            }))
            .ToArray();
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
