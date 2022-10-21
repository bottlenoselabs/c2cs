// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Reflection;
using C2CS.Data.C.Serialization;
using C2CS.ReadCodeC.Explore;
using C2CS.ReadCodeC.Parse;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS.ReadCodeC;

public static class Startup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Data
        services.AddSingleton<CJsonSerializer>();

        // Logic
        services.AddSingleton<ClangInstaller>();
        services.AddSingleton<ClangArgumentsBuilder>();
        services.AddSingleton<Parser>();
        services.AddSingleton<Explorer>();

        // Use case
        services.AddTransient<UseCaseReadCodeC>();
        services.AddSingleton<ReadCodeCValidator>();

        AddExploreHandlers(services);
    }

    private static void AddExploreHandlers(IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes().Where(p => !p.IsAbstract && typeof(ExploreHandler).IsAssignableFrom(p))
            .ToImmutableArray();
        foreach (var type in types)
        {
            services.AddSingleton(type, type);
        }
    }
}
