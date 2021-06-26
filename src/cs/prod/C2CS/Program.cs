// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace C2CS
{
	public static class Program
	{
		public static int Main(string[] args)
		{
			return CreateRootCommand().InvokeAsync(args).Result;
		}

		private static Command CreateRootCommand()
		{
			// Create a root command with some options
			var rootCommand = new RootCommand("C2CS - C to C# bindings code generator.");

			var abstractSyntaxTreeCommand = CommandAbstractSyntaxTreeC();
			rootCommand.Add(abstractSyntaxTreeCommand);

			var bindgenCSharpCommand = CommandBindgenCSharp();
			rootCommand.Add(bindgenCSharpCommand);

			return rootCommand;
		}

		private static Command CommandAbstractSyntaxTreeC()
		{
			var command =
				new Command("ast", "Dump the abstract syntax tree of a C `.h` file to a `.json` file.");

			var inputFileOption = new Option<FileInfo>(
				new[] {"--inputFile", "-i"},
				"Path of the input `.h` header file.")
			{
				IsRequired = true
			};
			command.AddOption(inputFileOption);

			var outputFileOption = new Option<FileInfo>(
				new[] {"--outputFile", "-o"},
				"Path of the output abstract syntax tree `.json` file.")
			{
				IsRequired = true
			};
			command.AddOption(outputFileOption);

			var automaticallyFindSoftwareDevelopmentKitOption = new Option<IEnumerable<string>?>(
				new[] {"--automaticallyFindSoftwareDevelopmentKit", "-f"},
				"Find software development kit for C/C++ automatically. Default is true.")
			{
				IsRequired = false
			};
			command.AddOption(automaticallyFindSoftwareDevelopmentKitOption);

			var includeDirectoriesOption = new Option<IEnumerable<string?>?>(
				new[] {"--includeDirectories", "-s"},
				"Search directories for `#include` usages to use when parsing C code.")
			{
				IsRequired = false
			};
			command.AddOption(includeDirectoriesOption);

			var ignoredFilesOption = new Option<IEnumerable<string?>?>(
				new[] {"--ignoredFiles", "-g"},
				"Header files to ignore.")
			{
				IsRequired = false
			};
			command.AddOption(ignoredFilesOption);

			var opaqueTypesOption = new Option<IEnumerable<string?>?>(
				new[] {"--opaqueTypes", "-p"},
				"Types by name that will be forced to be opaque.")
			{
				IsRequired = false
			};
			command.AddOption(opaqueTypesOption);

			var definesOption = new Option<IEnumerable<string>?>(
				new[] {"--defines", "-d"},
				"Object-like macros to use when parsing C code.")
			{
				IsRequired = false
			};
			command.AddOption(definesOption);

			var clangArgsOption = new Option<IEnumerable<string>?>(
				new[] {"--clangArgs", "-a"},
				"Additional Clang arguments to use when parsing C code.")
			{
				IsRequired = false
			};
			command.AddOption(clangArgsOption);

			command.Handler = CommandHandler.Create<
				FileInfo,
				FileInfo,
				bool?,
				IEnumerable<string?>?,
				IEnumerable<string?>?,
				IEnumerable<string?>?,
				IEnumerable<string?>?,
				IEnumerable<string?>?
			>(AbstractSyntaxTreeC);
			return command;
		}

		private static Command CommandBindgenCSharp()
		{
			var command = new Command("cs", "Generate C# bindings from a C abstract syntax tree `.json` file.");

			var inputFileOption = new Option<FileInfo>(
				new[] {"--inputFile", "-i"},
				"Path of the input abstract syntax tree `.json` file.")
			{
				IsRequired = true
			};
			command.AddOption(inputFileOption);

			var outputFileOption = new Option<FileInfo>(
				new[] {"--outputFile", "-o"},
				"Path of the output C# `.cs` file.")
			{
				IsRequired = true
			};
			command.AddOption(outputFileOption);

			var typeAliasesOption = new Option<IEnumerable<string?>?>(
				new[] {"--typeAliases", "-a"},
				"Types by name that will be remapped.")
			{
				IsRequired = false
			};
			command.AddOption(typeAliasesOption);

			var libraryNameOption = new Option<string?>(
				new[] {"--libraryName", "-l"},
				"The name of the dynamic link library (without the file extension) used for P/Invoke with C#.")
			{
				IsRequired = false
			};
			command.AddOption(libraryNameOption);

			command.Handler = CommandHandler.Create<FileInfo, FileInfo, IEnumerable<string?>?, string>(BindgenCSharp);
			return command;
		}

		// NOTE: parameter name must match full name of command line option
		private static void AbstractSyntaxTreeC(
			FileInfo inputFile,
			FileInfo outputFile,
			bool? automaticallyFindSoftwareDevelopmentKit,
			IEnumerable<string?>? includeDirectories,
			IEnumerable<string?>? ignoredFiles,
			IEnumerable<string?>? opaqueTypes,
			IEnumerable<string?>? defines,
			IEnumerable<string?>? clangArgs)
		{
			var request = new UseCases.AbstractSyntaxTreeC.Request(
				inputFile,
				outputFile,
				automaticallyFindSoftwareDevelopmentKit,
				includeDirectories,
				ignoredFiles,
				opaqueTypes,
				defines,
				clangArgs);
			var useCase = new UseCases.AbstractSyntaxTreeC.UseCase();
			useCase.Execute(request);
		}

		private static void BindgenCSharp(
			FileInfo inputFile,
			FileInfo outputFile,
			IEnumerable<string?>? typeAliases,
			string? libraryName)
		{
			var libraryName2 = string.IsNullOrEmpty(libraryName) ? string.Empty : libraryName;

			var request = new UseCases.BindgenCSharp.Request(
				inputFile, outputFile, typeAliases, libraryName2);
			var useCase = new UseCases.BindgenCSharp.UseCase();
			useCase.Execute(request);
		}
	}
}
