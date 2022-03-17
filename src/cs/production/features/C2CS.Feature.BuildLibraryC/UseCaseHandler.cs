// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BuildLibraryC;

public class UseCaseHandler : UseCaseHandler<Input, Output>
{
    protected override void Execute(Input input, Output output)
    {
        foreach (var buildTarget in input.Project.Targets)
        {
            Console.WriteLine(buildTarget);
        }
    }
}
