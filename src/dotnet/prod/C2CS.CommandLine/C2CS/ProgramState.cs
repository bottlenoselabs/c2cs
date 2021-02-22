// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;

namespace C2CS
{
    [SuppressMessage(
        "Microsoft.Design",
        "CA1051:DoNotDeclareVisibleInstanceFields",
        Justification = "Read-only plain old data structure.")]
    public readonly struct ProgramState
    {
        public readonly string InputFilePath;
        public readonly string OutputFilePath;
        public readonly string LibraryName;
        public readonly ImmutableArray<string> ClangArgs;
        public readonly Stopwatch Stopwatch;

        public ProgramState(
            string inputFilePath,
            string outputFilePath,
            bool isUnattended,
            string libraryName,
            ImmutableArray<string>? includeDirectories,
            ImmutableArray<string>? defineMacros,
            ImmutableArray<string>? additionalArgs)
        {
            InputFilePath = ProcessInputFilePath(inputFilePath);
            OutputFilePath = ProcessOutputFilePath(outputFilePath, isUnattended);
            LibraryName = ProcessLibraryName(libraryName);
            ClangArgs = ProcessClangArgs(includeDirectories, defineMacros, additionalArgs);
            Stopwatch = new Stopwatch();
        }

        private static string ProcessInputFilePath(string inputFilePath)
        {
            inputFilePath = Path.GetFullPath(inputFilePath);

            if (File.Exists(inputFilePath))
            {
                return inputFilePath;
            }

            throw new ProgramException($"File doesn't exist: {inputFilePath}");
        }

        private static string ProcessOutputFilePath(string outputFilePath, bool isUnattended)
        {
            outputFilePath = Path.GetFullPath(outputFilePath);

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

        private static string ProcessLibraryName(string libraryName)
        {
            if (!string.IsNullOrEmpty(libraryName))
            {
                return libraryName;
            }

            throw new ProgramException("Dynamic link library name can't be empty.");
        }

        private static ImmutableArray<string> ProcessClangArgs(
            ImmutableArray<string>? includeDirectories,
            ImmutableArray<string>? defineMacros,
            ImmutableArray<string>? additionalArgs)
        {
            var commandLineArgs = new List<string>();
            AddDefaultArgs(commandLineArgs);
            AddSystemIncludes(commandLineArgs);
            AddUserIncludes(commandLineArgs);
            AddUserMacroDefinitions(commandLineArgs);
            AddUserArgs(commandLineArgs);
            return commandLineArgs.ToImmutableArray();

            static void AddDefaultArgs(ICollection<string> args)
            {
                args.Add("--language=c");
                args.Add("--std=c11");
                args.Add("-Wno-pragma-once-outside-header");
                args.Add("-fno-blocks");
            }

            void AddUserIncludes(ICollection<string> args)
            {
                if (includeDirectories == null)
                {
                    return;
                }

                foreach (var includeDirectory in includeDirectories)
                {
                    var commandLineArg = "--include-directory=" + includeDirectory;
                    args.Add(commandLineArg);
                }
            }

            static void AddSystemIncludes(ICollection<string> commandLineArgs)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    AddSystemIncludesForMac(commandLineArgs);
                }
            }

            static void AddSystemIncludesForMac(ICollection<string> commandLineArgs)
            {
                if (!Directory.Exists("/Library/Developer/CommandLineTools"))
                {
                    throw new NotSupportedException("Please install CommandLineTools for macOS: `xcode-select --install`.");
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "xcrun",
                    Arguments = "--sdk macosx --show-sdk-path",
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                var process = Process.Start(processStartInfo)!;
                var softwareDevelopmentKitDirectoryPath =
                    process.StandardOutput.ReadToEnd().Replace(Environment.NewLine, string.Empty);
                process.WaitForExit();

                var systemIncludeDirectoryPath = $"{softwareDevelopmentKitDirectoryPath}/usr/include";
                var systemIncludeCommandLineArg = $"--include-directory={systemIncludeDirectoryPath}";
                commandLineArgs.Add(systemIncludeCommandLineArg);

                const string clangIncludeDirectoryPath = "/Library/Developer/CommandLineTools/usr/lib/clang/12.0.0/include";
                var clangIncludeCommandLineArg = $"--include-directory={clangIncludeDirectoryPath}";
                commandLineArgs.Add(clangIncludeCommandLineArg);
            }

            void AddUserMacroDefinitions(ICollection<string> args)
            {
                if (defineMacros == null)
                {
                    return;
                }

                foreach (var defineMacro in defineMacros)
                {
                    var commandLineArg = "--define-macro=" + defineMacro;
                    args.Add(commandLineArg);
                }
            }

            void AddUserArgs(ICollection<string> args)
            {
                if (additionalArgs == null)
                {
                    return;
                }

                foreach (var commandLineArg in additionalArgs)
                {
                    args.Add(commandLineArg);
                }
            }
        }
    }
}
