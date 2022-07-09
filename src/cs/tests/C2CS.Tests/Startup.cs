// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Linq;
using System.Reflection;
using C2CS.Tests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS.IntegrationTests;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        SourceDirectory.SetPath();
        var assembly = Assembly.GetExecutingAssembly();
        var assemblyTypes = assembly.GetTypes();
        var fixtureTypes = assemblyTypes.Where(x => typeof(TestFixture).IsAssignableFrom(x) && !x.IsAbstract);
        foreach (var type in fixtureTypes)
        {
            services.AddSingleton(type);
        }
    }
}
