// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using C2CS.Commands.BuildCLibrary.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace C2CS.Tests;

public static class TestHost
{
    private static readonly IHost Host = HostBuilder().Build();

    public static IServiceProvider Services => Host.Services;

    private static IHostBuilder HostBuilder()
    {
        var result = new HostBuilder()
            .BuildHostCommon()
            .ConfigureServices(ConfigureServices);

        return result;
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<CMakeLibraryBuilder>();
        services.AddSingleton<TestCSharpCode>();
        services.AddSingleton<TestFixtureCSharpCode>();
    }
}
