// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.Logging;

namespace C2CS.Feature.BuildLibraryC;

public class BuildLibraryUseCase : UseCase<BuildLibraryRequest, BuildLibraryInput, BuildLibraryResponse>
{
    public BuildLibraryUseCase(ILogger logger)
        : base("Build C library", logger, new BuildLibraryValidator())
    {
    }

    protected override BuildLibraryResponse? Execute(BuildLibraryInput input)
    {
        var targets = input.Project.Targets;
        if (targets.IsDefaultOrEmpty)
        {
            return null;
        }

        foreach (var buildTarget in input.Project.Targets)
        {
            Console.WriteLine(buildTarget);
        }

        return new BuildLibraryResponse();
    }
}
