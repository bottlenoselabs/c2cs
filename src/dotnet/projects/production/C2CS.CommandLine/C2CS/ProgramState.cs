// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;

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
            var commandLineArgs = new List<string>
            {
                "--language=c",
                "--std=c11",
                "-Wno-pragma-once-outside-header"
            };

            if (includeDirectories != null)
            {
                foreach (var includeDirectory in includeDirectories)
                {
                    var commandLineArg = "--include-directory=" + includeDirectory;
                    commandLineArgs.Add(commandLineArg);
                }
            }

            if (defineMacros != null)
            {
                foreach (var defineMacro in defineMacros)
                {
                    var commandLineArg = "--define-macro=" + defineMacro;
                    commandLineArgs.Add(commandLineArg);
                }
            }

            if (additionalArgs != null)
            {
                foreach (var commandLineArg in additionalArgs)
                {
                    commandLineArgs.Add(commandLineArg);
                }
            }

            return commandLineArgs.ToImmutableArray();
        }
    }
}
