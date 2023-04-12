// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using Microsoft.Extensions.Configuration;
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

        var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(filePath))
        {
            builder.AddJsonFile(filePath);
        }
        else
        {
            var jsonStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("C2CS.appsettings.json")!;
            builder.AddJsonStream(jsonStream);
        }

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
        services.AddHostedService<CommandLineHost>();
        services.AddSingleton<RootCommand, CommandLineInterface>();

        WriteCodeCSharp.Startup.ConfigureServices(services);
    }
}
