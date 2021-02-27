// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;

namespace C2CS
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			var rootCommand = CreateRootCommand();
			return rootCommand.InvokeAsync(args).Result;
		}

		private static Command CreateRootCommand()
		{
			// Create a root command with some options
			var rootCommand = new RootCommand("C2CS - C to C# bindings code generator.");

			var inputFilePathOption = new Option<string>(
				new[] {"--inputFilePath", "-i"},
				"File path of the input .h file.")
			{
				IsRequired = true
			};
			rootCommand.AddOption(inputFilePathOption);

			var additionalPathsOption = new Option<string>(
				new[] {"--additionalInputPaths", "-p"},
				"Directory paths and/or file paths of additional .h files to bundle together before parsing C code.")
			{
				IsRequired = false
			};
			rootCommand.AddOption(additionalPathsOption);

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
				"The name of the dynamic link library (without the file extension) used for P/Invoke with C#.")
			{
				IsRequired = false
			};
			rootCommand.AddOption(libraryNameOption);

			var includeDirectoriesOption = new Option<IEnumerable<string>?>(
				new[] {"--includeDirectories", "-s"},
				"Search directories for `#include` usages to use when parsing C code.")
			{
				IsRequired = false
			};
			rootCommand.AddOption(includeDirectoriesOption);

			var definesOption = new Option<IEnumerable<string>?>(
				new[] {"--defines", "-d"},
				"Object-like macros to use when parsing C code.")
			{
				IsRequired = false
			};
			rootCommand.AddOption(definesOption);

			var clangArgsOption = new Option<IEnumerable<string>?>(
				new[] {"--clangArgs", "-a"},
				"Additional Clang arguments to use when parsing C code.")
			{
				IsRequired = false
			};
			rootCommand.AddOption(clangArgsOption);

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
			IEnumerable<string>? defines = null,
			IEnumerable<string>? clangArgs = null,
			IEnumerable<string>? additionalInputPaths = null)
		{
			try
			{
				var request = new BindgenUseCaseRequest(
					inputFilePath,
					outputFilePath,
					unattended,
					libraryName,
					includeDirectories?.ToImmutableArray() ?? ImmutableArray<string>.Empty,
					defines?.ToImmutableArray() ?? ImmutableArray<string>.Empty,
					clangArgs?.ToImmutableArray() ?? ImmutableArray<string>.Empty,
					additionalInputPaths?.ToImmutableArray() ?? ImmutableArray<string>.Empty);

				var useCase = new BindgenUseCase();
				var response = useCase.Execute(request);
				Debug.Assert(response.OutputFilePath == request.OutputFilePath, "equal");
			}
			catch (UseCaseException)
			{
				Environment.Exit(-1);
			}
		}

		private delegate void StartDelegate(
			string inputFilePath,
			string outputFilePath,
			bool unattended,
			string libraryName,
			IEnumerable<string>? includeDirectories = null,
			IEnumerable<string>? defines = null,
			IEnumerable<string>? clangArgs = null,
			IEnumerable<string>? additionalInputPaths = null);
	}
}
