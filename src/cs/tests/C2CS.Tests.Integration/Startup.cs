// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace C2CS.IntegrationTests;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        my_c_library.Startup.ConfigureServices(services);
    }
}
