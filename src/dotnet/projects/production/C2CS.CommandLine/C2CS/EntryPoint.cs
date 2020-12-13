// Copyright (c) Craftwork Games. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace C2CS
{
    internal static class EntryPoint
    {
        private static int Main(string[] args)
        {
            var rootCommand = CreateRootCommand();
            return rootCommand.InvokeAsync(args).Result;
        }

        private static Command CreateRootCommand()
        {
            // Create a root command with some options
            var rootCommand = new RootCommand("C# Platform Invoke Code Generator");

            var inputFilePathOption = new Option<string>(
                new[] {"--inputFilePath", "-i"},
                "File path of the input .h file.")
            {
                IsRequired = true
            };
            rootCommand.AddOption(inputFilePathOption);

            var outputFilePathOption = new Option<string>(
                new[] {"--outputFilePath", "-o"},
                "File path of the output .cs file.")
            {
                IsRequired = true
            };
            rootCommand.AddOption(outputFilePathOption);

            var unattendedOption = new Option<bool>(
                new[] {"--unattended", "-u"},
                "Don't ask for further input.")
            {
                IsRequired = false
            };
            rootCommand.AddOption(unattendedOption);

            var libraryNameOption = new Option<string?>(
                new[] {"--libraryName", "-l"},
                "The name of the library. Default value is the file name of the input file path.")
            {
                IsRequired = false
            };
            rootCommand.AddOption(libraryNameOption);

            var includeDirectoriesOption = new Option<IEnumerable<string>?>(
                new[] {"--includeDirectories", "-s"},
                "Include directories to use for parsing C code.")
            {
                IsRequired = false
            };
            rootCommand.AddOption(includeDirectoriesOption);

            var defineMacrosOption = new Option<IEnumerable<string>?>(
                new[] {"--defineMacros", "-d"},
                "Macros to define for parsing C code.")
            {
                IsRequired = false
            };
            rootCommand.AddOption(defineMacrosOption);

            var additionalArgsOption = new Option<IEnumerable<string>?>(
                new[] {"--additionalArgs", "-a"},
                "Additional arguments for parsing C code.")
            {
                IsRequired = false
            };
            rootCommand.AddOption(additionalArgsOption);

            var startDelegate = new StartDelegate(Start);
            rootCommand.Handler = CommandHandler.Create(startDelegate);
            return rootCommand;
        }

        private delegate void StartDelegate(
            string inputFilePath,
            string outputFilePath,
            bool unattended,
            string? libraryName = null,
            IEnumerable<string>? includeDirectories = null,
            IEnumerable<string>? defineMacros = null,
            IEnumerable<string>? additionalArgs = null);

        private static void Start(
            string inputFilePath,
            string outputFilePath,
            bool unattended,
            string? libraryName = null,
            IEnumerable<string>? includeDirectories = null,
            IEnumerable<string>? defineMacros = null,
            IEnumerable<string>? additionalArgs = null)
        {
            var programState = new Program.State(
                inputFilePath,
                outputFilePath,
                unattended,
                libraryName,
                includeDirectories,
                defineMacros,
                additionalArgs);
            var program = new Program(programState);
            program.Execute();
        }
    }
}
