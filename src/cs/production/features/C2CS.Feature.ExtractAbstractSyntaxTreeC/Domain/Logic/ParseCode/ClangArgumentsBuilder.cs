// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Exceptions;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.ParseCode;

internal static class ClangArgumentsBuilder
{
    public static ImmutableArray<string> Build(
        bool automaticallyFindSoftwareDevelopmentKit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> defines,
        RuntimePlatform targetPlatform,
        ImmutableArray<string> additionalArgs)
    {
        var builder = ImmutableArray.CreateBuilder<string>();

        AddDefaults(builder, targetPlatform);
        AddUserIncludes(builder, includeDirectories);
        AddDefines(builder, defines);
        AddTargetTriple(builder, targetPlatform);
        AddAdditionalArgs(builder, additionalArgs);

        if (automaticallyFindSoftwareDevelopmentKit)
        {
            AddSystemIncludes(builder, targetPlatform);
        }

        return builder.ToImmutable();
    }

    private static void AddTargetTriple(ImmutableArray<string>.Builder args, RuntimePlatform targetPlatform)
    {
        var arch = targetPlatform.Architecture switch
        {
            RuntimeArchitecture.X64 => "x86_64",
            RuntimeArchitecture.X86 => "x86",
            RuntimeArchitecture.ARM32 => "arm",
            RuntimeArchitecture.ARM64 => "aarch64",
            _ => string.Empty
        };

        var vendor = targetPlatform.OperatingSystem switch
        {
            RuntimeOperatingSystem.Windows => "pc",
            RuntimeOperatingSystem.macOS => "apple",
            RuntimeOperatingSystem.iOS => "apple",
            RuntimeOperatingSystem.tvOS => "apple",
            _ => string.Empty
        };

        var os = targetPlatform.OperatingSystem switch
        {
            RuntimeOperatingSystem.Windows => "win32",
            RuntimeOperatingSystem.macOS => "darwin",
            RuntimeOperatingSystem.iOS => "ios",
            RuntimeOperatingSystem.tvOS => "tvos",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(arch) || string.IsNullOrEmpty(vendor) || string.IsNullOrEmpty(os))
        {
            return;
        }

        var targetTripleString = $"--target={arch}-{vendor}-{os}";
        args.Add(targetTripleString);
    }

    private static void AddDefaults(ImmutableArray<string>.Builder args, RuntimePlatform platform)
    {
        args.Add("--language=c");

        if (platform.OperatingSystem == RuntimeOperatingSystem.Linux)
        {
            args.Add("--std=gnu11");
        }
        else
        {
            args.Add("--std=c11");
        }

        args.Add("-Wno-pragma-once-outside-header");
        args.Add("-fno-blocks");
    }

    private static void AddUserIncludes(
        ImmutableArray<string>.Builder args, ImmutableArray<string> includeDirectories)
    {
        if (includeDirectories.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var searchDirectory in includeDirectories)
        {
            var commandLineArg = "--include-directory=" + searchDirectory;
            args.Add(commandLineArg);
        }
    }

    private static void AddDefines(ImmutableArray<string>.Builder args, ImmutableArray<string> defines)
    {
        if (defines.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var defineMacro in defines)
        {
            var commandLineArg = "--define-macro=" + defineMacro;
            args.Add(commandLineArg);
        }
    }

    private static void AddAdditionalArgs(ImmutableArray<string>.Builder args, ImmutableArray<string> additionalArgs)
    {
        if (additionalArgs.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var arg in additionalArgs)
        {
            args.Add(arg);
        }
    }

    private static void AddSystemIncludes(ImmutableArray<string>.Builder args, RuntimePlatform targetPlatform)
    {
        switch (targetPlatform.OperatingSystem)
        {
            case RuntimeOperatingSystem.Windows:
                AddSystemIncludesWindows(args);
                break;
            case RuntimeOperatingSystem.macOS:
            case RuntimeOperatingSystem.iOS:
            case RuntimeOperatingSystem.tvOS:
                AddSystemIncludesMac(args);
                break;
            case RuntimeOperatingSystem.Linux:
            case RuntimeOperatingSystem.Android:
                AddSystemIncludesLinux(args);
                break;
            case RuntimeOperatingSystem.Unknown:
                throw new NotSupportedException();
            case RuntimeOperatingSystem.FreeBSD:
            case RuntimeOperatingSystem.Browser:
            case RuntimeOperatingSystem.PlayStation:
            case RuntimeOperatingSystem.Xbox:
            case RuntimeOperatingSystem.Switch:
            default:
                throw new NotImplementedException();
        }
    }

    private static void AddSystemIncludesLinux(ImmutableArray<string>.Builder args)
    {
        // TODO: Is this always going to work? Be good if this was more bullet proof. If you know better fix it!
        const string directoryPath = "/usr/lib/gcc/x86_64-linux-gnu/9/include/";
        var systemIncludeCommandLineArg = $"-isystem{directoryPath}";
        args.Add(systemIncludeCommandLineArg);
    }

    private static void AddSystemIncludesWindows(ImmutableArray<string>.Builder clangArgumentsBuilder)
    {
        var sdkDirectoryPath =
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Windows Kits\10\Include");
        if (!string.IsNullOrEmpty(sdkDirectoryPath) && !Directory.Exists(sdkDirectoryPath))
        {
            throw new ClangException(
                "Please install the software development kit (SDK) for Windows 10: https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk/");
        }

        var sdkHighestVersionDirectoryPath = GetHighestVersionDirectoryPathFrom(sdkDirectoryPath);
        if (string.IsNullOrEmpty(sdkHighestVersionDirectoryPath))
        {
            throw new ClangException(
                $"Unable to find a Windows SDK version. Expected a Windows SDK version at '{sdkDirectoryPath}'. Do you need install the a software development kit for Windows? https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk/");
        }

        var systemIncludeCommandLineArgSdk = $@"-isystem{sdkHighestVersionDirectoryPath}\ucrt";
        clangArgumentsBuilder.Add(systemIncludeCommandLineArgSdk);

        var vsWhereFilePath =
            Environment.ExpandEnvironmentVariables(
                @"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe");
        var visualStudioInstallationDirectoryPath =
            "-latest -property installationPath".ShellCaptureOutput(fileName: vsWhereFilePath);
        if (!File.Exists(vsWhereFilePath) || string.IsNullOrEmpty(visualStudioInstallationDirectoryPath))
        {
            throw new ClangException(
                "Please install Visual Studio 2017 or later (community, professional, or enterprise).");
        }

        var mscvVersionsDirectoryPath = Path.Combine(visualStudioInstallationDirectoryPath, @"VC\Tools\MSVC");
        if (!Directory.Exists(mscvVersionsDirectoryPath))
        {
            throw new ClangException(
                $"Please install the Microsoft Visual C++ (MSVC) build tools for Visual Studio ({visualStudioInstallationDirectoryPath}).");
        }

        var mscvHighestVersionDirectoryPath = GetHighestVersionDirectoryPathFrom(mscvVersionsDirectoryPath);
        if (string.IsNullOrEmpty(mscvHighestVersionDirectoryPath))
        {
            throw new ClangException(
                $"Unable to find a version for Microsoft Visual C++ (MSVC) build tools for Visual Studio ({visualStudioInstallationDirectoryPath}).");
        }

        var mscvIncludeDirectoryPath = Path.Combine(mscvHighestVersionDirectoryPath, "include");
        if (!Directory.Exists(mscvIncludeDirectoryPath))
        {
            throw new ClangException(
                $"Please install Microsoft Visual C++ (MSVC) build tools for Visual Studio ({visualStudioInstallationDirectoryPath}).");
        }

        var systemIncludeCommandLineArg = $"-isystem{mscvIncludeDirectoryPath}";
        clangArgumentsBuilder.Add(systemIncludeCommandLineArg);
    }

    private static void AddSystemIncludesMac(ImmutableArray<string>.Builder clangArgumentsBuilder)
    {
        if (!Directory.Exists("/Library/Developer/CommandLineTools"))
        {
            throw new ClangException(
                "Please install CommandLineTools for macOS. This will install Clang. Use the command `xcode-select --install`.");
        }

        const string commandLineToolsClangDirectoryPath = "/Library/Developer/CommandLineTools/usr/lib/clang";
        var clangHighestVersionDirectoryPath =
            GetHighestVersionDirectoryPathFrom(commandLineToolsClangDirectoryPath);
        if (string.IsNullOrEmpty(clangHighestVersionDirectoryPath))
        {
            throw new ClangException(
                $"Unable to find a version of clang. Expected a version of clang at '{commandLineToolsClangDirectoryPath}'. Do you need to install CommandLineTools for macOS?");
        }

        var systemIncludeCommandLineArgClang = $"-isystem{clangHighestVersionDirectoryPath}/include";
        clangArgumentsBuilder.Add(systemIncludeCommandLineArgClang);

        var softwareDevelopmentKitDirectoryPath =
            "xcrun --sdk macosx --show-sdk-path".ShellCaptureOutput();
        if (!Directory.Exists(softwareDevelopmentKitDirectoryPath))
        {
            throw new ClangException(
                "Please install XCode for macOS. This will install the software development kit (SDK) which gives access to common C/C++/ObjC headers.");
        }

        var systemIncludeCommandLineArgSdk = $"-isystem{softwareDevelopmentKitDirectoryPath}/usr/include";
        clangArgumentsBuilder.Add(systemIncludeCommandLineArgSdk);
    }

    private static string GetHighestVersionDirectoryPathFrom(string sdkDirectoryPath)
    {
        var versionDirectoryPaths = Directory.EnumerateDirectories(sdkDirectoryPath);
        var result = string.Empty;
        var highestVersion = Version.Parse("0.0.0");

        foreach (var directoryPath in versionDirectoryPaths)
        {
            var versionStringIndex = directoryPath.LastIndexOf(Path.DirectorySeparatorChar);
            var versionString = directoryPath[(versionStringIndex + 1)..];
            if (!Version.TryParse(versionString, out var version))
            {
                continue;
            }

            if (version < highestVersion)
            {
                continue;
            }

            highestVersion = version;
            result = directoryPath;
        }

        return result;
    }
}
