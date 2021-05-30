// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.Json;
using C2CS.UseCases.Bindgen;

namespace C2CS
{
	public static class Program
	{
		public static int Main(string[] args)
		{
			var rootCommand = CreateRootCommand();
			var startDelegate = new StartDelegate(Start);
			rootCommand.Handler = CommandHandler.Create(startDelegate);
			return rootCommand.InvokeAsync(args).Result;
		}

		private static Command CreateRootCommand()
		{
			// Create a root command with some options
			var rootCommand = new RootCommand("C2CS - C to C# bindings code generator.");

			var configFileOption = new Option<string>(
				new[] {"--configFilePath", "-c"},
				"Path of the .json configuration file. If not specified, './config.json' is used.")
			{
				IsRequired = false
			};
			rootCommand.AddOption(configFileOption);

			var startDelegate = new StartDelegate(Start);
			rootCommand.Handler = CommandHandler.Create(startDelegate);
			return rootCommand;
		}

		private static void Start(string? configFilePath)
		{
			if (string.IsNullOrEmpty(configFilePath))
			{
				configFilePath = Path.Combine(Environment.CurrentDirectory, "config.json");
			}

			var configuration = BindgenConfiguration.LoadFromJson(configFilePath);
			var request = new BindgenRequest(configuration);
			var useCase = new BindgenUseCase();
			var response = useCase.Execute(request);
		}

		private delegate void StartDelegate(string? configFilePath);
	}
}
