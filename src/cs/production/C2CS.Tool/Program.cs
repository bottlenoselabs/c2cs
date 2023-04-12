// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using Microsoft.Extensions.Hosting;

namespace C2CS;

public static class Program
{
    public static int Main(string[] args)
    {
        using var host = Startup.CreateHost(args);
        host.Run();
        return Environment.ExitCode;
    }
}
