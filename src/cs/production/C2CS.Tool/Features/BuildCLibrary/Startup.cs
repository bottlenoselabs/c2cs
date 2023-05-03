// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Features.BuildCLibrary.Domain;
using C2CS.Features.BuildCLibrary.Input;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS.Features.BuildCLibrary;

public static class Startup
{
    public static void ConfigureServices(this IServiceCollection services)
    {
        services.AddSingleton<BuildCLibraryCommand>();
        services.AddSingleton<BuildCLibraryTool>();
        services.AddSingleton<BuildCLibraryInputSanitizer>();

        services.AddSingleton<CMakeLibraryBuilder>();
    }
}
