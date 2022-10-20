// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Options;

namespace C2CS.IntegrationTests.c_library.Fixtures.C;

public class ReadCCodeFixtureReader : IReaderCCode
{
    public ReaderOptionsCCode? Options { get; }

    public ReadCCodeFixtureReader()
    {
        Options = CreateOptions();
    }

    private static ReaderOptionsCCode CreateOptions()
    {
        var result = new ReaderOptionsCCode
        {
            InputFilePath = "./c/tests/c_library/include/_main.h",
            OutputFileDirectory = "./c/tests/c_library/ast",
            UserIncludeDirectories = new[] { "./c/production/c2cs/include" }.ToImmutableArray(),
            HeaderFilesBlocked = new[] { "parent_header.h" }.ToImmutableArray()
        };

        var platforms = ImmutableArray<TargetPlatform>.Empty;
        var hostOperatingSystem = Native.OperatingSystem;
        if (hostOperatingSystem == NativeOperatingSystem.Windows)
        {
            platforms = new[]
            {
                TargetPlatform.aarch64_pc_windows_msvc,
                TargetPlatform.x86_64_pc_windows_msvc
            }.ToImmutableArray();
        }
        else if (hostOperatingSystem == NativeOperatingSystem.macOS)
        {
            platforms = new[]
            {
                TargetPlatform.aarch64_apple_darwin,
                TargetPlatform.aarch64_apple_ios,
                TargetPlatform.x86_64_apple_darwin
            }.ToImmutableArray();
        }
        else if (hostOperatingSystem == NativeOperatingSystem.Linux)
        {
            platforms = new[]
            {
                TargetPlatform.aarch64_unknown_linux_gnu,
                TargetPlatform.x86_64_unknown_linux_gnu
            }.ToImmutableArray();
        }

        var builder = ImmutableDictionary.CreateBuilder<TargetPlatform, ReaderOptionsCCodePlatform>();
        foreach (var platform in platforms)
        {
            builder.Add(platform, new ReaderOptionsCCodePlatform());
        }

        result.Platforms = builder.ToImmutable();

        return result;
    }
}
