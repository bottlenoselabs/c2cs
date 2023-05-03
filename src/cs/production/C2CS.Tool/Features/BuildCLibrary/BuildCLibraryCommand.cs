// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using JetBrains.Annotations;

namespace C2CS.Features.BuildCLibrary;

[UsedImplicitly]
public sealed class BuildCLibraryCommand : Command
{
    private readonly BuildCLibraryTool _tool;

    public BuildCLibraryCommand(BuildCLibraryTool tool)
        : base(
            "library",
            "Build a C library for the purposes of FFI (foreign function interface) with other languages such as C#.")
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
