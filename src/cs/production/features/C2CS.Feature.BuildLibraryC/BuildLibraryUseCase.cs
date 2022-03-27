// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.Logging;

namespace C2CS.Feature.BuildLibraryC;

public class BuildLibraryUseCase : UseCase<BuildLibraryRequest, BuildLibraryInput, BuildLibraryOutput>
{
    public override string Name => "Build C library";

    public BuildLibraryUseCase(ILogger logger, IServiceProvider services, BuildLibraryValidator validator)
        : base(logger, services, validator)
    {
    }

    protected override void Execute(BuildLibraryInput input, BuildLibraryOutput output)
    {
        var targets = input.Project.Targets;
        if (targets.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var buildTarget in input.Project.Targets)
        {
            Console.WriteLine(buildTarget);
        }
    }
}
