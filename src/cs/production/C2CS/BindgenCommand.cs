// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.CommandLine;
using System.Text.Json;
using C2CS.Data;
using Json.Schema;
using Json.Schema.Generation;

namespace C2CS;

public class BindgenCommand : RootCommand
{
    private readonly Bindgen _bindgen;

    public BindgenCommand(Bindgen bindgen)
        : base("C2CS - C to C# bindings code generator.")
    {
        _bindgen = bindgen;
        Initialize();
    }

    private void Initialize()
    {
        var configurationOption = new Option<string>(
            new[] { "--configurationFilePath", "-c" },
            "File path of the configuration `.json` file.")
        {
            IsRequired = false
        };
        AddGlobalOption(configurationOption);

        var abstractSyntaxTreeCommand = new Command(
            "ast", "Dump the abstract syntax tree of a C `.h` file to one or more `.json` files per platform.");
        abstractSyntaxTreeCommand.AddOption(configurationOption);
        abstractSyntaxTreeCommand.SetHandler<string>(
            filePath => HandleAbstractSyntaxTreesC(filePath),
            configurationOption);
        AddCommand(abstractSyntaxTreeCommand);

        var bindgenCSharpCommand = new Command(
            "cs",
            "Generate a C# bindings `.cs` file from one or more C abstract syntax tree `.json` files per platform.");
        bindgenCSharpCommand.AddOption(configurationOption);
        bindgenCSharpCommand.SetHandler<string>(HandleBindgenCSharp, configurationOption);
        AddCommand(bindgenCSharpCommand);

        var configurationGenerateSchemaCommand = new Command(
            "schema", "Generate the `schema.json` file for the configuration in the working directory.");
        configurationGenerateSchemaCommand.SetHandler(GenerateSchema);
        AddCommand(configurationGenerateSchemaCommand);

        this.SetHandler<string>(Handle, configurationOption);
    }

    private void Handle(string configurationFilePath)
    {
        _bindgen.Execute(configurationFilePath);
    }

    private void HandleAbstractSyntaxTreesC(string configurationFilePath)
    {
        _bindgen.ReadCodeC(configurationFilePath);
    }

    private void HandleBindgenCSharp(string configurationFilePath)
    {
        _bindgen.WriteCodeCSharp(configurationFilePath);
    }

    private static void GenerateSchema()
    {
        var schemaBuilder = new JsonSchemaBuilder().FromType<BindgenConfiguration>();
        var schema = schemaBuilder.Build();
        var json = JsonSerializer.Serialize(schema);
        File.WriteAllText("schema.json", json);
    }
}
