// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using System.Reflection;
using C2CS.Contexts.ReadCodeC.Domain.Parse.Diagnostics;
using C2CS.Foundation.Diagnostics;
using Microsoft.Extensions.Logging;

namespace C2CS.Contexts.ReadCodeC.Domain.Parse;

public partial class ClangArgumentsBuilder
{
    private readonly ILogger<ClangArgumentsBuilder> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly List<FileSystemInfo> _temporaryLinkPaths = new();

    public ClangArgumentsBuilder(ILogger<ClangArgumentsBuilder> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public ImmutableArray<string> Build(
        DiagnosticsSink diagnostics,
        TargetPlatform targetPlatform,
        ParseOptions options,
        bool isCPlusPlus)
    {
        var args = ImmutableArray.CreateBuilder<string>();

        AddDefaults(args, targetPlatform, isCPlusPlus);
        AddUserIncludeDirectories(args, options.UserIncludeDirectories);
        AddDefines(args, options.MacroObjectsDefines);
        AddTargetTriple(args, targetPlatform);
        AddAdditionalArgs(args, options.AdditionalArguments);
        AddSystemIncludeDirectories(
            args,
            targetPlatform,
            options.SystemIncludeDirectories,
            options.Frameworks,
            options.IsEnabledFindSystemHeaders,
            diagnostics);

        return args.ToImmutable();
    }

    public ImmutableDictionary<string, string> GetLinkedPaths()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>();
        foreach (var linkPath in _temporaryLinkPaths)
        {
            builder.Add(linkPath.FullName, linkPath.LinkTarget!);
        }

        return builder.ToImmutable();
    }

    public void Cleanup()
    {
        foreach (var linkPath in _temporaryLinkPaths)
        {
            linkPath.Delete();
        }
    }

    private void AddTargetTriple(ImmutableArray<string>.Builder args, TargetPlatform platform)
    {
        var targetTripleString = $"--target={platform}";
        args.Add(targetTripleString);
    }

    private static void AddDefaults(ImmutableArray<string>.Builder args, TargetPlatform platform, bool isCPlusPlus)
    {
        if (isCPlusPlus)
        {
            args.Add("--language=c++");

            if (platform.OperatingSystem == NativeOperatingSystem.Linux)
            {
                args.Add("--std=gnu++11");
            }
            else
            {
                args.Add("--std=c++11");
            }
        }
        else
        {
            args.Add("--language=c");

            if (platform.OperatingSystem == NativeOperatingSystem.Linux)
            {
                args.Add("--std=gnu11");
            }
            else
            {
                args.Add("--std=c11");
            }

            args.Add("-fblocks");
            args.Add("-Wno-pragma-once-outside-header");
        }
    }

    private static void AddUserIncludeDirectories(
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

    private void AddSystemIncludeDirectories(
        ImmutableArray<string>.Builder args,
        TargetPlatform targetPlatform,
        ImmutableArray<string> directories,
        ImmutableArray<string> frameworks,
        bool isEnabledFindSystemHeaders,
        DiagnosticsSink diagnostics)
    {
        ImmutableArray<string> systemIncludeDirectories;
        if (isEnabledFindSystemHeaders)
        {
            systemIncludeDirectories = SystemIncludeDirectories(targetPlatform, directories, frameworks);
        }
        else
        {
            systemIncludeDirectories = directories;
        }

        var builder = ImmutableArray.CreateBuilder<string>();
        foreach (var directory in systemIncludeDirectories)
        {
            if (_fileSystem.Directory.Exists(directory))
            {
                builder.Add(directory);
            }
            else
            {
                LogMissingSystemIncludeDirectory(directory);
            }
        }

        var systemIncludeDirectoriesThatExist = builder.ToImmutableArray();
        if (systemIncludeDirectoriesThatExist.IsDefaultOrEmpty)
        {
            var diagnostic = new MissingSystemHeadersDiagnostic(targetPlatform);
            diagnostics.Add(diagnostic);
        }
        else
        {
            foreach (var directory in systemIncludeDirectoriesThatExist)
            {
                args.Add($"-isystem{directory}");
            }
        }
    }

    private ImmutableArray<string> SystemIncludeDirectories(
        TargetPlatform targetPlatform,
        ImmutableArray<string> manualSystemIncludeDirectories,
        ImmutableArray<string> frameworks)
    {
        var hostOperatingSystem = Native.OperatingSystem;
        var hostArchitecture = Native.Architecture;
        var targetOperatingSystem = targetPlatform.OperatingSystem;
        var directories = ImmutableArray.CreateBuilder<string>();

        switch (hostOperatingSystem)
        {
            case NativeOperatingSystem.Windows:
            {
                SystemIncludeDirectoriesHostWindows(targetOperatingSystem, directories);
                break;
            }

            case NativeOperatingSystem.macOS:
            {
                SystemIncludeDirectoriesHostMac(frameworks, targetOperatingSystem, directories);
                break;
            }

            case NativeOperatingSystem.Linux:
            {
                SystemIncludeDirectoriesHostLinux(targetPlatform, targetOperatingSystem, hostArchitecture, directories);
                break;
            }
        }

        directories.AddRange(manualSystemIncludeDirectories);
        return directories.ToImmutable();
    }

    private void SystemIncludeDirectoriesHostWindows(
        NativeOperatingSystem targetSystem, ImmutableArray<string>.Builder directories)
    {
        if (targetSystem == NativeOperatingSystem.Windows)
        {
            SystemIncludeDirectoriesTargetWindows(directories);
        }
    }

    private void SystemIncludeDirectoriesHostMac(
        ImmutableArray<string> frameworks,
        NativeOperatingSystem targetSystem,
        ImmutableArray<string>.Builder directories)
    {
        if (targetSystem == NativeOperatingSystem.macOS)
        {
            SystemIncludesDirectoriesTargetMac(directories, frameworks);
        }
        else if (targetSystem == NativeOperatingSystem.iOS)
        {
            SystemIncludesDirectoriesTargetIPhone(directories, frameworks);
        }
    }

    private static void SystemIncludeDirectoriesHostLinux(
        TargetPlatform targetPlatform,
        NativeOperatingSystem targetSystem,
        NativeArchitecture hostArchitecture,
        ImmutableArray<string>.Builder directories)
    {
        if (targetSystem == NativeOperatingSystem.Linux)
        {
            SystemIncludeDirectoriesTargetLinux(hostArchitecture, targetPlatform.Architecture, directories);
        }
    }

    private void SystemIncludeDirectoriesTargetWindows(ImmutableArray<string>.Builder directories)
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

        var systemIncludeCommandLineArgSdk = $@"{sdkHighestVersionDirectoryPath}\ucrt";
        directories.Add(systemIncludeCommandLineArgSdk);

        var vsWhereFilePath =
            Environment.ExpandEnvironmentVariables(
                @"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe");
        var shellOutput = "-latest -property installationPath".ExecuteShell(fileName: vsWhereFilePath);
        var visualStudioInstallationDirectoryPath = shellOutput.Output;
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

        directories.Add(mscvIncludeDirectoryPath);
    }

    private void SystemIncludesDirectoriesTargetMac(
        ImmutableArray<string>.Builder directories, ImmutableArray<string> frameworks)
    {
        var shellOutput = "xcrun --sdk macosx --show-sdk-path".ExecuteShell();
        var sdkPath = shellOutput.Output;
        if (!_fileSystem.Directory.Exists(sdkPath))
        {
            throw new ClangException(
                "Please install XCode or CommandLineTools for macOS. This will install the software development kit (SDK) for macOS which gives access to common C/C++/ObjC headers.");
        }

        directories.Add($"{sdkPath}/usr/include");
        AddFrameworks(directories, frameworks, sdkPath);
    }

    private void SystemIncludesDirectoriesTargetIPhone(
        ImmutableArray<string>.Builder directories, ImmutableArray<string> frameworks)
    {
        var shellOutput = "xcrun --sdk iphoneos --show-sdk-path".ExecuteShell();
        var sdkPath = shellOutput.Output;
        if (!_fileSystem.Directory.Exists(sdkPath))
        {
            throw new ClangException(
                "Please install XCode for macOS. This will install the software development kit (SDK) for iOS which gives access to common C/C++/ObjC headers.");
        }

        directories.Add($"{sdkPath}/usr/include");
        AddFrameworks(directories, frameworks, sdkPath);
    }

    private static void SystemIncludeDirectoriesTargetLinux(
        NativeArchitecture hostArchitecture,
        NativeArchitecture targetArchitecture,
        ImmutableArray<string>.Builder directories)
    {
        // Cross platform headers are in: /usr/[ARCH]-linux-gnu/include
        //  For Ubuntu, cross platform toolchain (includes headers) are installed via packages:
        //  - gcc-x86-64-linux-gnu (ARCH = x86_64)
        //  - gcc-aarch64-linux-gnu (ARCH = aarch64)
        //  - gcc-i686-linux-gnu (ARCH = i686)
        // Host headers are in /usr/include/[ARCH]-linux-gnu

        if (targetArchitecture == hostArchitecture)
        {
            if (targetArchitecture == NativeArchitecture.X64)
            {
                directories.Add("/usr/include/x86_64-linux-gnu");
            }
            else if (targetArchitecture == NativeArchitecture.ARM64)
            {
                directories.Add("/usr/include/aarch64-linux-gnu");
            }
            else if (targetArchitecture == NativeArchitecture.X86)
            {
                directories.Add("/usr/include/i686-linux-gnu");
            }
        }
        else
        {
            if (targetArchitecture == NativeArchitecture.X64)
            {
                directories.Add("/usr/x86_64-linux-gnu/include");
            }
            else if (targetArchitecture == NativeArchitecture.ARM64)
            {
                directories.Add("/usr/aarch64-linux-gnu/include");
            }
            else if (targetArchitecture == NativeArchitecture.X86)
            {
                directories.Add("/usr/i686-linux-gnu/include");
            }
        }
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

    private void AddFrameworks(
        ImmutableArray<string>.Builder directories, ImmutableArray<string> frameworks, string sdkPath)
    {
        var frameworkLinks = new List<(string FrameworkName, string LinkTargetPath)>();
        foreach (var framework in frameworks)
        {
            var systemFrameworkPath = Path.Combine(
                sdkPath, "System", "Library", "Frameworks", framework + ".framework", "Headers");
            if (_fileSystem.Directory.Exists(systemFrameworkPath))
            {
                frameworkLinks.Add((framework, systemFrameworkPath));
            }
        }

        if (frameworkLinks.Count > 0)
        {
            var temporarySystemIncludesDirectory = GetTemporarySystemIncludesDirectory();
            if (!directories.Contains(temporarySystemIncludesDirectory))
            {
                directories.Add(temporarySystemIncludesDirectory);
            }

            foreach (var (frameworkName, linkTargetPath) in frameworkLinks)
            {
                var linkPath = _fileSystem.Path.Combine(temporarySystemIncludesDirectory, frameworkName);
                FileSystemInfo fileInfo = new FileInfo(linkPath);
                if (fileInfo.LinkTarget != null)
                {
                    fileInfo.Delete();
                }

                fileInfo = File.CreateSymbolicLink(linkPath, linkTargetPath);
                _temporaryLinkPaths.Add(fileInfo);
            }
        }
    }

    private string GetTemporarySystemIncludesDirectory()
    {
        var path = _fileSystem.Path;
        var directory = _fileSystem.Directory;

        var directoryName = Assembly.GetEntryAssembly()!.GetName().Name;
        var temporaryPath = path.Combine(path.GetTempPath(), directoryName);
        if (!directory.Exists(temporaryPath))
        {
            directory.CreateDirectory(temporaryPath);
        }

        return temporaryPath;
    }

    [LoggerMessage(0, LogLevel.Warning, "- Could not find system include directory: {DirectoryPath}")]
    private partial void LogMissingSystemIncludeDirectory(string directoryPath);
}
