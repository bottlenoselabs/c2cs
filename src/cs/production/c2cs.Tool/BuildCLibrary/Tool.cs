// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using bottlenoselabs.Common.Tools;
using Microsoft.Extensions.Logging;
using CMakeLibraryBuilder = C2CS.BuildCLibrary.CMakeLibraryBuilder;

namespace C2CS.BuildCLibrary;

public class Tool(
    ILogger<Tool> logger,
    InputSanitizer inputSanitizer,
    IFileSystem fileSystem,
    CMakeLibraryBuilder cMakeLibraryBuilder) : Tool<InputUnsanitized, InputSanitized, Output>(logger, inputSanitizer, fileSystem)
{
    public ImmutableArray<string> AdditionalCMakeArguments { get; set; } = ImmutableArray<string>.Empty;

    public new Output Run(string configurationFilePath)
    {
        return base.Run(configurationFilePath);
    }

    protected override void Execute(
        InputSanitized input,
        Output output)
    {
        BeginStep("Building C library");

        _ = cMakeLibraryBuilder.BuildLibrary(input, AdditionalCMakeArguments);

        EndStep();
    }
}
