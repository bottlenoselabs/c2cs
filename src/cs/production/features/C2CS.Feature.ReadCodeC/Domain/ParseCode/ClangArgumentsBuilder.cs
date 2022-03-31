// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;

namespace C2CS.Feature.ReadCodeC.Domain.ParseCode;

public class ClangArgumentsBuilder
{
    private readonly IFileSystem _fileSystem;

    public ClangArgumentsBuilder(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public ImmutableArray<string>? Build(
        bool automaticallyFindSoftwareDevelopmentKit,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> defines,
        TargetPlatform targetPlatform,
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

    private void AddTargetTriple(ImmutableArray<string>.Builder args, TargetPlatform platform)
    {
        var targetTripleString = $"--target={platform}";
        args.Add(targetTripleString);
    }

    private void AddDefaults(ImmutableArray<string>.Builder args, TargetPlatform platform)
    {
        args.Add("--language=c");

        if (platform.OperatingSystem == TargetOperatingSystem.Linux)
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

    private void AddUserIncludes(
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

    private void AddDefines(ImmutableArray<string>.Builder args, ImmutableArray<string> defines)
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

    private void AddAdditionalArgs(ImmutableArray<string>.Builder args, ImmutableArray<string> additionalArgs)
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

    private void AddSystemIncludes(ImmutableArray<string>.Builder args, TargetPlatform targetPlatform)
    {
        var hostOperatingSystem = Platform.OperatingSystem;
        var targetOperatingSystem = targetPlatform.OperatingSystem;

        switch (hostOperatingSystem)
        {
            case TargetOperatingSystem.Windows when targetOperatingSystem is
                TargetOperatingSystem.Windows:
            {
                AddSystemIncludesWindows(args);
                break;
            }

            case TargetOperatingSystem.macOS when targetOperatingSystem is
                TargetOperatingSystem.macOS:
            {
                AddSystemIncludesMac(args);
                break;
            }

            case TargetOperatingSystem.Linux when targetOperatingSystem is
                TargetOperatingSystem.Linux:
            {
                AddSystemIncludesLinux(args);
                break;
            }
        }
    }

    private static void AddSystemIncludeDirectory(ImmutableArray<string>.Builder args, string directoryPath)
    {
        var systemIncludeCommandLineArg = $"-isystem{directoryPath}";
        args.Add(systemIncludeCommandLineArg);
    }

    private void AddSystemIncludesLinux(ImmutableArray<string>.Builder args)
    {
        AddSystemIncludeDirectory(args, "/usr/include");
        // TODO: Is this always going to work? Be good if this was more bullet proof. If you know better fix it!
        AddSystemIncludeDirectory(args, "/usr/lib/gcc/x86_64-linux-gnu/9/include/");
    }

    private void AddSystemIncludesWindows(ImmutableArray<string>.Builder args)
    {
        var sdkDirectoryPath =
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Windows Kits\10\Include");
        if (!string.IsNullOrEmpty(sdkDirectoryPath) && !_fileSystem.Directory.Exists(sdkDirectoryPath))
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
        args.Add(systemIncludeCommandLineArgSdk);

        var vsWhereFilePath =
            Environment.ExpandEnvironmentVariables(
                @"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe");
        var visualStudioInstallationDirectoryPath =
            "-latest -property installationPath".ShellCaptureOutput(fileName: vsWhereFilePath);
        if (!_fileSystem.File.Exists(vsWhereFilePath) || string.IsNullOrEmpty(visualStudioInstallationDirectoryPath))
        {
            throw new ClangException(
                "Please install Visual Studio 2017 or later (community, professional, or enterprise).");
        }

        var mscvVersionsDirectoryPath = Path.Combine(visualStudioInstallationDirectoryPath, @"VC\Tools\MSVC");
        if (!_fileSystem.Directory.Exists(mscvVersionsDirectoryPath))
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
        if (!_fileSystem.Directory.Exists(mscvIncludeDirectoryPath))
        {
            throw new ClangException(
                $"Please install Microsoft Visual C++ (MSVC) build tools for Visual Studio ({visualStudioInstallationDirectoryPath}).");
        }

        AddSystemIncludeDirectory(args, mscvIncludeDirectoryPath);
    }

    private void AddSystemIncludesMac(ImmutableArray<string>.Builder args)
    {
        if (!_fileSystem.Directory.Exists("/Library/Developer/CommandLineTools"))
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
        args.Add(systemIncludeCommandLineArgClang);

        var softwareDevelopmentKitDirectoryPath =
            "xcrun --sdk macosx --show-sdk-path".ShellCaptureOutput();
        if (!_fileSystem.Directory.Exists(softwareDevelopmentKitDirectoryPath))
        {
            throw new ClangException(
                "Please install XCode for macOS. This will install the software development kit (SDK) which gives access to common C/C++/ObjC headers.");
        }

        AddSystemIncludeDirectory(args, $"{softwareDevelopmentKitDirectoryPath}/usr/include");
    }

    private string GetHighestVersionDirectoryPathFrom(string sdkDirectoryPath)
    {
        var versionDirectoryPaths = _fileSystem.Directory.EnumerateDirectories(sdkDirectoryPath);
        var result = string.Empty;
        var highestVersion = Version.Parse("0.0.0");

        foreach (var directoryPath in versionDirectoryPaths)
        {
            var versionStringIndex = directoryPath.LastIndexOf(_fileSystem.Path.DirectorySeparatorChar);
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
