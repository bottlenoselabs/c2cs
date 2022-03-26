// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using C2CS.Feature.BindgenCSharp;
using C2CS.Feature.ExtractAbstractSyntaxTreeC;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS;

internal class CommandLineInterface : RootCommand
{
    private readonly ConfigurationService _configurationService;
    private readonly IServiceProvider _serviceProvider;

    public CommandLineInterface(
        ConfigurationService configurationService,
        IServiceProvider serviceProvider)
        : base("C2CS - C to C# bindings code generator.")
    {
        _configurationService = configurationService;
        _serviceProvider = serviceProvider;

        var configurationOption = new Option(
            new[] { "--configuration", "-c" },
            "File path of the configuration `.json` file.",
            typeof(string),
            () => "config.json");
        AddGlobalOption(configurationOption);

        var abstractSyntaxTreeCommand = CommandAbstractSyntaxTreeC();
        Add(abstractSyntaxTreeCommand);

        var bindgenCSharpCommand = CommandBindgenCSharp();
        Add(bindgenCSharpCommand);
    }

    private Command CommandAbstractSyntaxTreeC()
    {
        var command = new Command(
            "ast", "Dump the abstract syntax tree of a C `.h` file to a `.json` file.");
        command.SetHandler(() =>
        {
            var configuration = _configurationService.Read("config.json");
            var request = configuration.ExtractAbstractSyntaxTreeC;
            var useCase = _serviceProvider.GetService<ExtractAbstractSyntaxTreeUseCase>()!;
            useCase.Execute(request);
        });
        return command;
    }

    private Command CommandBindgenCSharp()
    {
        var command = new Command(
            "cs", "Generate C# bindings from a C abstract syntax tree `.json` file.");
        command.SetHandler(() =>
        {
            var configuration = _configurationService.Read("config.json");
            var request = configuration.BindgenCSharp;
            var useCase = _serviceProvider.GetService<BindgenUseCase>()!;
            useCase.Execute(request);
        });
        return command;
    }
}
