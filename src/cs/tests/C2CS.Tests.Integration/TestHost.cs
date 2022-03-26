// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using Microsoft.Extensions.Hosting;

namespace C2CS.IntegrationTests;

public static class TestHost
{
    public static IServiceProvider Services => Host.Services;

    private static readonly IHost Host = new HostBuilder()
        .BuildHostCommon().ConfigureServices(Startup.ConfigureServices).Build();
}
