// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using C2CS;

public static class ClangArgumentsBuilder
{
    public static ImmutableArray<string> Build(
        bool automaticallyFindSoftwareDevelopmentKit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> defines,
        int? bitness,
        ImmutableArray<string> additionalArgs)
    {
        var builder = ImmutableArray.CreateBuilder<string>();

        AddDefault(builder);
        AddBitness(
            builder,
            bitness ?? (RuntimeInformation.OSArchitecture is Architecture.Arm64 or Architecture.X64 ? 64 : 32));
        AddUserIncludes(builder, includeDirectories);
        AddDefines(builder, defines);
        AddAdditionalArgs(builder, additionalArgs);

        if (automaticallyFindSoftwareDevelopmentKit)
        {
            AddSystemIncludes(builder);
        }

        return builder.ToImmutable();
    }

    private static void AddDefault(ImmutableArray<string>.Builder args)
    {
        args.Add("--language=c");

        if (Platform.OperatingSystem == RuntimeOperatingSystem.Linux)
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

    private static void AddBitness(ImmutableArray<string>.Builder args, int bitness)
    {
        args.Add($"-m{bitness}");
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

    private static void AddSystemIncludes(ImmutableArray<string>.Builder args)
    {
        var runtime = Platform.OperatingSystem;
        switch (runtime)
        {
            case RuntimeOperatingSystem.Windows:
                AddSystemIncludesWindows(args);
                break;
            case RuntimeOperatingSystem.macOS:
                AddSystemIncludesMac(args);
                break;
            case RuntimeOperatingSystem.Linux:
                AddSystemIncludesLinux(args);
                break;
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
	        "-latest -property installationPath".RunCommandWithCapturingStandardOutput(fileName: vsWhereFilePath);
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
            "xcrun --sdk macosx --show-sdk-path".RunCommandWithCapturingStandardOutput();
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
