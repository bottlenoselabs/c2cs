// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace C2CS.UseCases.Bindgen
{
    public readonly struct BindgenInput
    {
        public readonly string InputFilePath;
        public readonly string OutputFilePath;
        public readonly string ClassName;
        public readonly string LibraryName;
        public readonly bool PrintAbstractSyntaxTree;
        public readonly IEnumerable<string> OpaqueTypes;
        public readonly ImmutableArray<string> ClangArgs;

        public BindgenInput(
            string inputFilePath,
            string outputFilePath,
            bool isUnattended,
            string className,
            string libraryName,
            bool printAbstractSyntaxTree,
            bool autoFindSdk,
            IEnumerable<string> opaqueTypes,
            ImmutableArray<string> searchDirectories,
            ImmutableArray<string> defineMacros,
            ImmutableArray<string> additionalArgs)
        {
            InputFilePath = ProcessInputPaths(inputFilePath);
            OutputFilePath = ProcessOutputFilePath(outputFilePath, isUnattended);
            ClassName = ProcessClassName(className);
            LibraryName = ProcessLibraryName(libraryName);
            OpaqueTypes = opaqueTypes;
            ClangArgs = ProcessClangArgs(autoFindSdk, searchDirectories, defineMacros, additionalArgs);
            PrintAbstractSyntaxTree = printAbstractSyntaxTree;
        }

        private static string ProcessInputPaths(string inputFilePath)
        {
            var path = Path.GetFullPath(inputFilePath.Trim());
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File does not exist: {path}");
            }

            return path;
        }

        private static string ProcessOutputFilePath(string outputFilePath, bool isUnattended)
        {
            outputFilePath = Path.GetFullPath(outputFilePath.Trim());

            if (!File.Exists(outputFilePath))
            {
                return outputFilePath;
            }

            if (!isUnattended)
            {
                Console.WriteLine($"The file already exists: {outputFilePath}");
                Console.WriteLine("Do you want to overwrite it? [Y/N]");

                var consoleKeyInfo = Console.ReadKey();
                if (consoleKeyInfo.Key == ConsoleKey.Y)
                {
                    File.Delete(outputFilePath);
                }

                Console.WriteLine();
            }
            else
            {
                File.Delete(outputFilePath);
            }

            return outputFilePath;
        }

        private static string ProcessClassName(string className)
        {
            var value = className.Trim();

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            throw new UseCaseInputException("C# class name can't be null or empty.");
        }

        private static string ProcessLibraryName(string libraryName)
        {
            var value = libraryName.Trim();

            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            throw new UseCaseInputException("Dynamic link library name can't be null or empty.");
        }

        private static ImmutableArray<string> ProcessClangArgs(
            bool autoFindSdk,
            ImmutableArray<string> includeDirectories,
            ImmutableArray<string> defines,
            ImmutableArray<string> clangArgs)
        {
            var commandLineArgs = new List<string>();

            AddArgsDefault(commandLineArgs);

            if (autoFindSdk)
            {
                AddSdkSystemIncludes(commandLineArgs);
            }

            AddArgsUserIncludes(commandLineArgs, includeDirectories);
            AddArgsUserDefines(commandLineArgs, defines);
            AddArgsUserClangArgs(commandLineArgs, clangArgs);

            return commandLineArgs.ToImmutableArray();

            static void AddArgsDefault(ICollection<string> args)
            {
                args.Add("--language=c");
                args.Add("--std=c11");
                args.Add("-Wno-pragma-once-outside-header");
                args.Add("-fno-blocks");
            }

            static void AddSdkSystemIncludes(ICollection<string> commandLineArgs)
            {
                var runtime = NativeRuntime.RuntimePlatform;
                switch (runtime)
                {
                    case RuntimePlatform.Windows:
                        AddSystemIncludesWindows(commandLineArgs);
                        break;
                    case RuntimePlatform.macOS:
                        AddSystemIncludesMac(commandLineArgs);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            static void AddArgsUserIncludes(ICollection<string> args, ImmutableArray<string> searchDirectories)
            {
                if (searchDirectories.IsDefaultOrEmpty)
                {
                    return;
                }

                foreach (var searchDirectory in searchDirectories)
                {
                    var commandLineArg = "--include-directory=" + searchDirectory;
                    args.Add(commandLineArg);
                }
            }

            static void AddSystemIncludesWindows(ICollection<string> commandLineArgs)
            {
                var sdkDirectoryPath =
                    Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Windows Kits\10\Include");
                if (string.IsNullOrEmpty(sdkDirectoryPath))
                {
                    throw new UseCaseInputException(
                        "Please install the software development kit (SDK) for Windows 10: https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk/");
                }

                var sdkHighestVersionDirectoryPath = GetHighestVersionDirectoryPathFrom(sdkDirectoryPath);
                var systemIncludeCommandLineArgSdk = $@"-isystem{sdkHighestVersionDirectoryPath}\ucrt";
                commandLineArgs.Add(systemIncludeCommandLineArgSdk);

                var vsWhereFilePath = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe");
                if (!File.Exists(vsWhereFilePath))
                {
                    throw new UseCaseInputException(
                        "Please install Visual Studio 2017 or later (community, professional, or enterprise).");
                }

                var visualStudioInstallationDirectoryPath = "-latest -property installationPath".ShCaptureStandardOutput(fileName: vsWhereFilePath);
                var mscvVersionsDirectoryPath = Path.Combine(visualStudioInstallationDirectoryPath, @"VC\Tools\MSVC");
                var mscvHighestVersionDirectoryPath = GetHighestVersionDirectoryPathFrom(mscvVersionsDirectoryPath);
                var mscvIncludeDirectoryPath = Path.Combine(mscvHighestVersionDirectoryPath, "include");
                if (!Directory.Exists(mscvIncludeDirectoryPath))
                {
                    throw new UseCaseInputException(
                        "Please install the Microsoft Visual C++ (MSCV) build tool through Visual Studio installer (modification of Visual Studio installed components).");
                }

                var systemIncludeCommandLineArg = $"-isystem{mscvIncludeDirectoryPath}";
                commandLineArgs.Add(systemIncludeCommandLineArg);
            }

            static void AddSystemIncludesMac(ICollection<string> commandLineArgs)
            {
                if (!Directory.Exists("/Library/Developer/CommandLineTools"))
                {
                    throw new UseCaseInputException(
                        "Please install CommandLineTools for macOS: `xcode-select --install`.");
                }

                const string commandLineToolsClangDirectoryPath = "/Library/Developer/CommandLineTools/usr/lib/clang";
                var clangHighestVersionDirectoryPath = GetHighestVersionDirectoryPathFrom(commandLineToolsClangDirectoryPath);
                var systemIncludeCommandLineArgClang = $"-isystem{clangHighestVersionDirectoryPath}/include";
                commandLineArgs.Add(systemIncludeCommandLineArgClang);

                var softwareDevelopmentKitDirectoryPath = "xcrun --sdk macosx --show-sdk-path".ShCaptureStandardOutput();
                if (!Directory.Exists(softwareDevelopmentKitDirectoryPath))
                {
                    throw new UseCaseInputException(
                        "Please install XCode for macOS to get the software development kit (SDK) to get access to common C/C++/ObjC headers.");
                }

                var systemIncludeCommandLineArgSdk = $"-isystem{softwareDevelopmentKitDirectoryPath}/usr/include";
                commandLineArgs.Add(systemIncludeCommandLineArgSdk);
            }

            static void AddArgsUserDefines(ICollection<string> args, ImmutableArray<string> defineMacros)
            {
                if (defineMacros.IsDefaultOrEmpty)
                {
                    return;
                }

                foreach (var defineMacro in defineMacros)
                {
                    var commandLineArg = "--define-macro=" + defineMacro;
                    args.Add(commandLineArg);
                }
            }

            static void AddArgsUserClangArgs(ICollection<string> args, ImmutableArray<string> additionalArgs)
            {
                if (additionalArgs.IsDefaultOrEmpty)
                {
                    return;
                }

                foreach (var commandLineArg in additionalArgs)
                {
                    args.Add(commandLineArg);
                }
            }
        }

        private static string GetHighestVersionDirectoryPathFrom(string sdkDirectoryPath)
        {
            var versionDirectoryPaths = Directory.EnumerateDirectories(sdkDirectoryPath);
            var result = string.Empty;
            Version highestVersion = Version.Parse("0.0.0");

            foreach (var directoryPath in versionDirectoryPaths)
            {
                var versionStringIndex = directoryPath.LastIndexOf(Path.DirectorySeparatorChar);
                var versionString = directoryPath[(versionStringIndex + 1)..];
                var version = Version.Parse(versionString);
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
}
