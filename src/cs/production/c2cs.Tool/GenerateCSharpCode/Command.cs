// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using JetBrains.Annotations;

namespace C2CS.GenerateCSharpCode;

[UsedImplicitly]
public sealed class Command : System.CommandLine.Command
{
    private readonly Tool _tool;

    public Command(Tool tool)
        : base(
            "generate",
            "Generate C# code from a cross-platform abstract syntax tree (AST) '.json' file.")
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
        var output = _tool.Run(configurationFilePath);
        if (!output.IsSuccess)
        {
            Environment.Exit(1);
        }
    }
}
