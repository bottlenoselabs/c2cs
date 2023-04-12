// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.CommandLine;
using C2CS.WriteCodeCSharp;

namespace C2CS;

internal sealed class CommandLineInterface : RootCommand
{
    private readonly WriteCodeCSharpTool _tool;

    public CommandLineInterface(WriteCodeCSharpTool tool)
        : base("C2CS - C to C# bindings code generator.")
    {
        _tool = tool;

        var configurationFilePathOption = new Option<string>(
            "--config", "The file path configure C# bindgen.");
        configurationFilePathOption.SetDefaultValue("config.json");
        AddOption(configurationFilePathOption);
        this.SetHandler(Main, configurationFilePathOption);
    }

    private void Main(string configurationFilePath)
    {
        _tool.Run(configurationFilePath);
    }
}
