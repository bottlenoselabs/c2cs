// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BuildLibraryC;

public class UseCase : UseCase<Request, Input, Response>
{
    public UseCase()
        : base(new Validator())
    {
    }

    protected override Response? Execute(Input input)
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

        return new Response();
    }
}
