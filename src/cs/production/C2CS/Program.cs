// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
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

			var inputFileOption = new Option<string>(
				new[] {"--inputFile", "-i"},
				"Path of the input `.h` header file.")
			{
				IsRequired = true
			};
			command.AddOption(inputFileOption);

			var outputFileOption = new Option<string>(
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

			var bitOption = new Option<int>(
				new[] {"--bitness", "-b"},
				"The bitness to parse the C code as. Default is the current architecture of host operating system. E.g. the default for x64 Windows is `64`. Possible values are `32` where pointers are 4 bytes, or `64` where pointers are 8 bytes.")
			{
				IsRequired = false
			};
			command.AddOption(bitOption);

			var clangArgsOption = new Option<IEnumerable<string>?>(
				new[] {"--clangArgs", "-a"},
				"Additional Clang arguments to use when parsing C code.")
			{
				IsRequired = false
			};
			command.AddOption(clangArgsOption);

			command.Handler = CommandHandler.Create<
				string,
				string,
				bool?,
				IEnumerable<string?>?,
				IEnumerable<string?>?,
				IEnumerable<string?>?,
				IEnumerable<string?>?,
				int?,
				IEnumerable<string?>?
			>(AbstractSyntaxTreeC);
			return command;
		}

		private static Command CommandBindgenCSharp()
		{
			var command = new Command("cs", "Generate C# bindings from a C abstract syntax tree `.json` file.");

			var inputFileOption = new Option<string>(
				new[] {"--inputFile", "-i"},
				"Path of the input abstract syntax tree `.json` file.")
			{
				IsRequired = true
			};
			command.AddOption(inputFileOption);

			var outputFileOption = new Option<string>(
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

			var ignoredTypesOption = new Option<IEnumerable<string?>?>(
				new[] {"--ignoredTypes", "-g"},
				"Types by name that will be ignored; types are ignored after remapping type names.")
			{
				IsRequired = false
			};
			command.AddOption(ignoredTypesOption);

			var libraryNameOption = new Option<string?>(
				new[] {"--libraryName", "-l"},
				"The name of the dynamic link library (without the file extension) used for P/Invoke with C#.")
			{
				IsRequired = false
			};
			command.AddOption(libraryNameOption);

			var classNameOption = new Option<string?>(
				new[] {"--className", "-c"},
				"The name of the C# static class.")
			{
				IsRequired = false
			};
			command.AddOption(classNameOption);

			var namespacesOption = new Option<IEnumerable<string?>>(
				new[] {"--namespaces", "-n"},
				"Additional namespaces to inject near the top of C# file as using statements.")
			{
				IsRequired = false
			};
			command.AddOption(namespacesOption);

			command.Handler = CommandHandler.Create<
				string,
				string,
				IEnumerable<string?>?,
				IEnumerable<string?>?,
				string?,
				string?,
				IEnumerable<string?>?
			>(BindgenCSharp);
			return command;
		}

		// NOTE: parameter name must match full name of command line option
		private static void AbstractSyntaxTreeC(
			string inputFile,
			string outputFile,
			bool? automaticallyFindSoftwareDevelopmentKit,
			IEnumerable<string?>? includeDirectories,
			IEnumerable<string?>? ignoredFiles,
			IEnumerable<string?>? opaqueTypes,
			IEnumerable<string?>? defines,
			int? bitness,
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
				bitness,
				clangArgs);
			var useCase = new UseCases.AbstractSyntaxTreeC.UseCase();
			var response = useCase.Execute(request);
			if (response.Status == UseCaseOutputStatus.Failure)
			{
				Environment.Exit(1);
			}
		}

		private static void BindgenCSharp(
			string inputFile,
			string outputFile,
			IEnumerable<string?>? typeAliases,
			IEnumerable<string?>? ignoredTypes,
			string? libraryName,
			string? className,
			IEnumerable<string?>? namespaces)
		{
			var libraryName2 = string.IsNullOrEmpty(libraryName) ? string.Empty : libraryName;
			var className2 = string.IsNullOrEmpty(className) ? string.Empty : className;

			var request = new UseCases.BindgenCSharp.Request(
				inputFile,
				outputFile,
				typeAliases,
				ignoredTypes,
				libraryName2,
				className2,
				namespaces);
			var useCase = new UseCases.BindgenCSharp.UseCase();
			var response = useCase.Execute(request);
			if (response.Status == UseCaseOutputStatus.Failure)
			{
				Environment.Exit(1);
			}
		}
	}
}
