// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BuildLibraryC;

public static class Program
{
    public static int Main(string[]? args = null)
    {
        var input = Input.GetFrom(args);
        var handler = new UseCaseHandler();
        var output = handler.Execute(input);
        return output.Status == UseCaseOutputStatus.Success ? 0 : 1;
    }
}
