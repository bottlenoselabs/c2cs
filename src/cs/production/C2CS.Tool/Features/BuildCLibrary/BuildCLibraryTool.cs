// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.IO.Abstractions;
using C2CS.Features.BuildCLibrary.Domain;
using C2CS.Features.BuildCLibrary.Input;
using C2CS.Features.BuildCLibrary.Input.Sanitized;
using C2CS.Features.BuildCLibrary.Input.Unsanitized;
using C2CS.Features.BuildCLibrary.Output;
using C2CS.Foundation.Tool;
using Microsoft.Extensions.Logging;

namespace C2CS.Features.BuildCLibrary;

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
