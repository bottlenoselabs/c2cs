// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation.UseCases;
using Microsoft.Extensions.Logging;

namespace C2CS.Contexts.BuildLibraryC;

public class BuildLibraryUseCase : UseCase<BuildLibraryCConfiguration, BuildLibraryInput, BuildLibraryOutput>
{
    public BuildLibraryUseCase(
        ILogger<BuildLibraryUseCase> logger, IServiceProvider services, BuildLibraryValidator validator)
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
