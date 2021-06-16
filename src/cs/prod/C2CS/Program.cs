// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

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
			var abstractSyntaxTreeCommand =
				new Command("ast", "Dump the abstract syntax tree of a C `.h` file to a `.json` file.");

			var inputFileOption = new Option<FileInfo>(
				new[] {"--inputFile", "-i"},
				"Path of the input `.h` file.")
			{
				IsRequired = true
			};
			abstractSyntaxTreeCommand.AddOption(inputFileOption);

			var outputFileOption = new Option<FileInfo>(
				new[] {"--outputFile", "-o"},
				"Path of the output abstract syntax tree `.json` file.")
			{
				IsRequired = true
			};
			abstractSyntaxTreeCommand.AddOption(outputFileOption);

			var configFileOption = new Option<FileInfo>(
				new[] {"--configurationFile", "-c"},
				"Path of the `.json` configuration file. If not specified default values for configuration are used. Configuration is intended for advanced scenarios; for more information see developer documentation.")
			{
				IsRequired = false
			};
			abstractSyntaxTreeCommand.AddOption(configFileOption);

			abstractSyntaxTreeCommand.Handler = CommandHandler.Create<FileInfo, FileInfo, FileInfo>(AbstractSyntaxTreeC);
			return abstractSyntaxTreeCommand;
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

			var configFileOption = new Option<FileInfo>(
				new[] {"--configurationFile", "-c"},
				"Path of the `.json` configuration file. If not specified default values for configuration are used. Configuration is intended for advanced scenarios; for more information see developer documentation.")
			{
				IsRequired = false
			};
			command.AddOption(configFileOption);

			command.Handler = CommandHandler.Create<FileInfo, FileInfo, FileInfo>(BindgenCSharp);
			return command;
		}

		// NOTE: parameter name must match full name of command line option
		private static void AbstractSyntaxTreeC(FileInfo inputFile, FileInfo outputFile, FileInfo configurationFile)
		{
			var request = new UseCases.AbstractSyntaxTreeC.Request(inputFile, outputFile, configurationFile);
			var useCase = new UseCases.AbstractSyntaxTreeC.UseCase();
			useCase.Execute(request);
		}

		private static void BindgenCSharp(FileInfo inputFile, FileInfo outputFile, FileInfo configurationFile)
		{
			var request = new UseCases.BindgenCSharp.Request(inputFile, outputFile, configurationFile);
			var useCase = new UseCases.BindgenCSharp.UseCase();
			useCase.Execute(request);
		}
	}
}
