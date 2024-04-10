// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using bottlenoselabs.Common.Tools;
using C2CS.Commands.BuildCLibrary.Input;
using C2CS.Commands.BuildCLibrary.Input.Sanitized;
using C2CS.Commands.BuildCLibrary.Input.Unsanitized;
using C2CS.Commands.BuildCLibrary.Output;
using Microsoft.Extensions.Logging;
using CMakeLibraryBuilder = C2CS.Commands.BuildCLibrary.Domain.CMakeLibraryBuilder;

namespace C2CS.Commands.BuildCLibrary;

public class BuildCLibraryTool : Tool<BuildCLibraryOptions, BuildCLibraryInput, BuildCLibraryOutput>
{
    private readonly CMakeLibraryBuilder _cMakeLibraryBuilder;

    public ImmutableArray<string> AdditionalCMakeArguments { get; set; } = ImmutableArray<string>.Empty;

    public BuildCLibraryTool(
        ILogger<BuildCLibraryTool> logger,
        BuildCLibraryInputSanitizer inputSanitizer,
        IFileSystem fileSystem,
        CMakeLibraryBuilder cMakeLibraryBuilder)
        : base(logger, inputSanitizer, fileSystem)
    {
        _cMakeLibraryBuilder = cMakeLibraryBuilder;
    }

    protected override void Execute(
        BuildCLibraryInput input,
        BuildCLibraryOutput output)
    {
        BeginStep("Building C library");

        _cMakeLibraryBuilder.BuildLibrary(input, AdditionalCMakeArguments);

        EndStep();
    }
}
