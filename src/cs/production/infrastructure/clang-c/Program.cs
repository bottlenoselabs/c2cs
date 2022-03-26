// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

internal static class Program
{
    private static void Main()
    {
        C2CS.Program.Main(new[] { "ast" });
        C2CS.Program.Main(new[] { "cs" });
    }
}
