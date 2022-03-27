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
    private readonly ConfigurationJsonSerializer _configurationJsonSerializer;
    private readonly IServiceProvider _serviceProvider;

    public CommandLineInterface(
        ConfigurationJsonSerializer configurationJsonSerializer,
        IServiceProvider serviceProvider)
        : base("C2CS - C to C# bindings code generator.")
    {
        _configurationJsonSerializer = configurationJsonSerializer;
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

        this.SetHandler(Handle);
    }

    private void Handle()
    {
        HandleAbstractSyntaxTreeC();
        HandleBindgenCSharp();
    }

    private Command CommandAbstractSyntaxTreeC()
    {
        var command = new Command(
            "ast", "Dump the abstract syntax tree of a C `.h` file to a `.json` file.");
        command.SetHandler(HandleAbstractSyntaxTreeC);
        return command;
    }

    private void HandleAbstractSyntaxTreeC()
    {
        var configuration = _configurationJsonSerializer.Read("config.json");
        var request = configuration.ExtractAbstractSyntaxTreeC;
        var useCase = _serviceProvider.GetService<ExtractAbstractSyntaxTreeUseCase>()!;
        useCase.Execute(request);
    }

    private Command CommandBindgenCSharp()
    {
        var command = new Command(
            "cs", "Generate C# bindings from one or more C abstract syntax tree `.json` files.");
        command.SetHandler(HandleBindgenCSharp);
        return command;
    }

    private void HandleBindgenCSharp()
    {
        var configuration = _configurationJsonSerializer.Read("config.json");
        var request = configuration.BindgenCSharp;
        var useCase = _serviceProvider.GetService<BindgenUseCase>()!;
        useCase.Execute(request);
    }
}
