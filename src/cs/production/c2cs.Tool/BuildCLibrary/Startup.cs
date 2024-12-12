// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace C2CS.BuildCLibrary;

public static class Startup
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        _ = services.AddSingleton<Command>();
        _ = services.AddSingleton<Tool>();
        _ = services.AddSingleton<InputSanitizer>();
        _ = services.AddSingleton<CMakeLibraryBuilder>();
    }
}
