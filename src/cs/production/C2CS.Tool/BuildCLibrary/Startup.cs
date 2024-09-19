// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.DependencyInjection;
using CMakeLibraryBuilder = C2CS.BuildCLibrary.CMakeLibraryBuilder;

namespace C2CS.BuildCLibrary;

public static class Startup
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddSingleton<Command>();
        services.AddSingleton<Tool>();
        services.AddSingleton<InputSanitizer>();

        services.AddSingleton<CMakeLibraryBuilder>();
    }
}
