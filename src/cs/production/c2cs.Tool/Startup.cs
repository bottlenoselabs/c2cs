// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
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
            _ = builder.AddJsonFile(filePath);
        }
        else
        {
            var jsonStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("C2CS.appsettings.json")!;
            _ = builder.AddJsonStream(jsonStream);
        }

        foreach (var originalSource in sources)
        {
            _ = builder.Add(originalSource);
        }
    }

    private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder builder)
    {
        _ = builder.ClearProviders();
        _ = builder.AddSimpleConsole();
        _ = builder.AddConfiguration(context.Configuration.GetSection("Logging"));
    }

    private static void ConfigureServices(IServiceCollection services, string[]? args)
    {
        _ = services.AddSingleton(new CommandLineArgumentsProvider(args ?? Environment.GetCommandLineArgs()));
        _ = services.AddSingleton<IFileSystem, FileSystem>();
        _ = services.AddHostedService<CommandLineHost>();
        _ = services.AddSingleton<System.CommandLine.RootCommand, RootCommand>();

        GenerateCSharpCode.Startup.ConfigureServices(services);
        BuildCLibrary.Startup.ConfigureServices(services);
    }
}
