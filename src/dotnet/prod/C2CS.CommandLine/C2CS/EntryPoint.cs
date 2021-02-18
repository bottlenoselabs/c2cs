// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

			var libraryNameOption = new Option<string>(
				new[] {"--libraryName", "-l"},
				"The name of the dynamic link library.")
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

		private static void Start(
			string inputFilePath,
			string outputFilePath,
			bool unattended,
			string libraryName,
			IEnumerable<string>? includeDirectories = null,
			IEnumerable<string>? defineMacros = null,
			IEnumerable<string>? additionalArgs = null)
		{
			try
			{
				var programState = new ProgramState(
					inputFilePath,
					outputFilePath,
					unattended,
					libraryName,
					includeDirectories?.ToImmutableArray(),
					defineMacros?.ToImmutableArray(),
					additionalArgs?.ToImmutableArray());

				var program = new Program(programState);
				program.Execute();
			}
			catch (ProgramException e)
			{
				Console.Error.WriteLine(e.Message);
				Environment.Exit(-1);
			}
		}

		private delegate void StartDelegate(
			string inputFilePath,
			string outputFilePath,
			bool unattended,
			string libraryName,
			IEnumerable<string>? includeDirectories = null,
			IEnumerable<string>? defineMacros = null,
			IEnumerable<string>? additionalArgs = null);
	}
}
