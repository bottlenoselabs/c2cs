// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using C2CS.Data.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace C2CS;

public static class Startup
{
    public static IHost CreateHost(string[] args)
    {
        return new HostBuilder()
            .ConfigureDefaults(args)
            .UseConsoleLifetime()
            .BuildHostCommon(args)
            .Build();
    }

    public static IHostBuilder BuildHostCommon(this IHostBuilder builder, string[]? args = null)
    {
        return builder
            .ConfigureAppConfiguration(ConfigureAppConfiguration)
            .ConfigureLogging(ConfigureLogging)
            .ConfigureServices(services => ConfigureServices(services, args));
    }

    private static void ConfigureAppConfiguration(IConfigurationBuilder builder)
    {
        AddDefaultConfiguration(builder);
    }

    private static void AddDefaultConfiguration(IConfigurationBuilder builder)
    {
        var sources = builder.Sources.ToImmutableArray();
        builder.Sources.Clear();
        var jsonStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("C2CS.appsettings.json");
        builder.AddJsonStream(jsonStream);

        foreach (var originalSource in sources)
        {
            builder.Add(originalSource);
        }
    }

    private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.AddSimpleConsole();
        builder.AddConfiguration(context.Configuration.GetSection("Logging"));
    }

    private static void ConfigureServices(IServiceCollection services, string[]? args)
    {
        services.AddSingleton(new CommandLineArgumentsProvider(args ?? Environment.GetCommandLineArgs()));
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddHostedService<CommandLineService>();
        services.AddSingleton<RootCommand, CommandLineInterface>();
        services.AddSingleton<BindgenConfigurationJsonSerializer>();

        Contexts.ReadCodeC.Startup.ConfigureServices(services);
        Contexts.WriteCodeCSharp.Startup.ConfigureServices(services);
        Contexts.BuildLibraryC.Startup.ConfigureServices(services);
    }
}
