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

        var configurationOption = new Option<string>(
            new[] { "--configurationFilePath", "-c" },
            () => "config.json",
            "File path of the configuration `.json` file.")
        {
            IsRequired = false
        };
        AddGlobalOption(configurationOption);

        var abstractSyntaxTreeCommand = new Command(
            "ast", "Dump the abstract syntax tree of a C `.h` file to a `.json` file.");
        abstractSyntaxTreeCommand.SetHandler<string>(HandleAbstractSyntaxTreeC, configurationOption);
        Add(abstractSyntaxTreeCommand);

        var bindgenCSharpCommand = new Command(
            "cs", "Generate C# bindings from one or more C abstract syntax tree `.json` files.");
        bindgenCSharpCommand.SetHandler<string>(HandleBindgenCSharp, configurationOption);
        Add(bindgenCSharpCommand);

        this.SetHandler<string>(Handle, configurationOption);
    }

    private void Handle(string configurationFilePath)
    {
        HandleAbstractSyntaxTreeC(configurationFilePath);
        HandleBindgenCSharp(configurationFilePath);
    }

    private void HandleAbstractSyntaxTreeC(string configurationFilePath)
    {
        var configuration = _configurationJsonSerializer.Read(configurationFilePath);
        var request = configuration.ExtractAbstractSyntaxTreeC;
        var useCase = _serviceProvider.GetService<ExtractAbstractSyntaxTreeUseCase>()!;
        useCase.Execute(request);
    }

    private void HandleBindgenCSharp(string configurationFilePath)
    {
        var configuration = _configurationJsonSerializer.Read(configurationFilePath);
        var request = configuration.BindgenCSharp;
        var useCase = _serviceProvider.GetService<BindgenUseCase>()!;
        useCase.Execute(request);
    }
}
