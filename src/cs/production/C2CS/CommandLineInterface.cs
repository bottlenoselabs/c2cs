// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using C2CS.Data;
using C2CS.Data.Serialization;
using C2CS.Feature.ReadCodeC;
using C2CS.Feature.WriteCodeCSharp;
using Json.Schema;
using Json.Schema.Generation;
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
            "File path of the configuration `.json` file.")
        {
            IsRequired = false
        };

        var abstractSyntaxTreeCommand = new Command(
            "ast", "Dump the abstract syntax tree of a C `.h` file to one or more `.json` files per platform.");
        abstractSyntaxTreeCommand.AddOption(configurationOption);
        abstractSyntaxTreeCommand.SetHandler<string>(HandleAbstractSyntaxTreesC, configurationOption);
        AddCommand(abstractSyntaxTreeCommand);

        var bindgenCSharpCommand = new Command(
            "cs", "Generate a C# bindings `.cs` file from one or more C abstract syntax tree `.json` files per platform.");
        bindgenCSharpCommand.AddOption(configurationOption);
        bindgenCSharpCommand.SetHandler<string>(HandleBindgenCSharp, configurationOption);
        AddCommand(bindgenCSharpCommand);

        var configurationGenerateSchemaCommand = new Command(
            "schema", "Generate the `schema.json` file for the configuration in the working directory.");
        configurationGenerateSchemaCommand.SetHandler(GenerateSchema);
        this.SetHandler<string>(Handle, configurationOption);
        AddCommand(configurationGenerateSchemaCommand);
    }

    private void Handle(string configurationFilePath)
    {
        HandleAbstractSyntaxTreesC(configurationFilePath);
        HandleBindgenCSharp(configurationFilePath);
    }

    private void HandleAbstractSyntaxTreesC(string configurationFilePath)
    {
        if (string.IsNullOrEmpty(configurationFilePath))
        {
            configurationFilePath = "config.json";
        }

        var configuration = _configurationJsonSerializer.Read(configurationFilePath);
        var request = configuration.ReadC;
        if (request == null)
        {
            return;
        }

        var useCase = _serviceProvider.GetService<ReadCodeCUseCase>()!;
        useCase.Execute(request);
    }

    private void HandleBindgenCSharp(string configurationFilePath)
    {
        if (string.IsNullOrEmpty(configurationFilePath))
        {
            configurationFilePath = "config.json";
        }

        var configuration = _configurationJsonSerializer.Read(configurationFilePath);
        var request = configuration.WriteCSharp;
        if (request == null)
        {
            return;
        }

        var useCase = _serviceProvider.GetService<WriteCodeCSharpUseCase>()!;
        useCase.Execute(request);
    }

    private static void GenerateSchema()
    {
        var schemaBuilder = new JsonSchemaBuilder().FromType<Configuration>();
        var schema = schemaBuilder.Build();
        var json = JsonSerializer.Serialize(schema);
        File.WriteAllText("schema.json", json);
    }
}
