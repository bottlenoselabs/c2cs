// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS;
using C2CS.Options;
using JetBrains.Annotations;

[PublicAPI]
public class ReaderCCode : IReaderCCode
{
    public ReaderCCodeOptions Options { get; set; } = new();

    public ReaderCCode()
    {
        Options.InputHeaderFilePath =
            "../../../src/cs/examples/helloworld/helloworld-compile-c-library-and-generate-bindings/my_c_library/include/my_c_library.h";
        Options.OutputAbstractSyntaxTreesFileDirectory =
            "../../../src/cs/examples/helloworld/helloworld-compile-c-library-and-generate-bindings/my_c_library/ast";

        ConfigurePlatforms(Options);
    }

    private static void ConfigurePlatforms(ReaderCCodeOptions options)
    {
        var platforms = new Dictionary<TargetPlatform, ReaderCCodeOptionsPlatform>();

        var hostOperatingSystem = Native.OperatingSystem;
        switch (hostOperatingSystem)
        {
            case NativeOperatingSystem.Windows:
                ConfigureHostOsWindows(options, platforms);
                break;
            case NativeOperatingSystem.macOS:
                ConfigureHostOsMac(options, platforms);
                break;
            case NativeOperatingSystem.Linux:
                ConfigureHostOsLinux(options, platforms);
                break;
            default:
                throw new NotImplementedException();
        }

        options.Platforms = platforms.ToImmutableDictionary();
    }

    private static void ConfigureHostOsWindows(
        ReaderCCodeOptions options,
        Dictionary<TargetPlatform, ReaderCCodeOptionsPlatform> platforms)
    {
        platforms.Add(TargetPlatform.aarch64_pc_windows_msvc, new ReaderCCodeOptionsPlatform());
        platforms.Add(TargetPlatform.x86_64_pc_windows_msvc, new ReaderCCodeOptionsPlatform());
    }

    private static void ConfigureHostOsMac(
        ReaderCCodeOptions options,
        Dictionary<TargetPlatform, ReaderCCodeOptionsPlatform> platforms)
    {
        platforms.Add(TargetPlatform.aarch64_apple_darwin, new ReaderCCodeOptionsPlatform());
        platforms.Add(TargetPlatform.x86_64_apple_darwin, new ReaderCCodeOptionsPlatform());
    }

    private static void ConfigureHostOsLinux(
        ReaderCCodeOptions options,
        Dictionary<TargetPlatform, ReaderCCodeOptionsPlatform> platforms)
    {
        platforms.Add(TargetPlatform.aarch64_unknown_linux_gnu, new ReaderCCodeOptionsPlatform());
        platforms.Add(TargetPlatform.x86_64_unknown_linux_gnu, new ReaderCCodeOptionsPlatform());
    }
}
