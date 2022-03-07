// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Help;

namespace C2CS;

public static class Program
{
    public static int Main(string[] args)
    {
        var rootCommand = CreateRootCommand();
        if (args.Length != 0)
        {
            return rootCommand.Invoke(args);
        }

        var helpBuilder = new HelpBuilder(LocalizationResources.Instance, Console.WindowWidth);
        helpBuilder.Write(rootCommand, Console.Out);
        return 0;
    }

    private static Command CreateRootCommand()
    {
        var rootCommand = new RootCommand(
            "C2CS - C to C# bindings code generator.");

        var configurationOption = new Option(
            new[] { "--configuration", "-c" },
            "File path of the configuration `.json` file.",
            typeof(string),
            () => "config.json");
        rootCommand.AddGlobalOption(configurationOption);

        var abstractSyntaxTreeCommand = CommandAbstractSyntaxTreeC();
        rootCommand.Add(abstractSyntaxTreeCommand);

        var bindgenCSharpCommand = CommandBindgenCSharp();
        rootCommand.Add(bindgenCSharpCommand);

        return rootCommand;
    }

    private static Command CommandAbstractSyntaxTreeC()
    {
        var command = new Command(
            "ast", "Dump the abstract syntax tree of a C `.h` file to a `.json` file.");
        command.SetHandler(ExtractAbstractSyntaxTreeC);
        return command;
    }

    private static Command CommandBindgenCSharp()
    {
        var command = new Command(
            "cs", "Generate C# bindings from a C abstract syntax tree `.json` file.");
        command.SetHandler(BindgenCSharp);
        return command;
    }

    private static void ExtractAbstractSyntaxTreeC()
    {
        var c = Configuration.LoadFrom("config.json");
        var configuration = c.ExtractAbstractSyntaxTreeC;
        if (configuration == null)
        {
            throw new UseCaseException("The configuration for `ast` is null.");
        }

        var request = new Feature.ExtractAbstractSyntaxTreeC.Input(configuration);
        var useCase = new Feature.ExtractAbstractSyntaxTreeC.Handler();
        useCase.Execute(request);
    }

    private static void BindgenCSharp()
    {
        var c = Configuration.LoadFrom("config.json");
        var configuration = c.BindgenCSharp;
        if (configuration == null)
        {
            throw new UseCaseException("The configuration for `cs` is null.");
        }

        var request = new Feature.BindgenCSharp.Input(configuration);
        var useCase = new Feature.BindgenCSharp.Handler();
        useCase.Execute(request);
    }
}
