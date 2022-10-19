// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;
using C2CS.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace C2CS;

public static class Startup
{
    internal static readonly PluginHost PluginHost = new(new Logger<PluginHost>(new LoggerFactory()));

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
        services.AddHostedService<CommandLineHost>();
        services.AddSingleton<RootCommand, CommandLineInterface>();
        services.AddSingleton<PluginHost>();

        Contexts.ReadCodeC.Startup.ConfigureServices(services);
        Contexts.WriteCodeCSharp.Startup.ConfigureServices(services);

        TryLoadPlugins(services, string.Empty);
    }

    private static void TryLoadPlugins(IServiceCollection services, string? pluginsFileDirectoryPath)
    {
        var searchFileDirectoryPath = pluginsFileDirectoryPath ?? Path.Combine(Environment.CurrentDirectory, "plugins");
        PluginHost.LoadPlugins(searchFileDirectoryPath);

        foreach (var pluginContext in PluginHost.Plugins)
        {
            var isPluginLoaded = TryLoadPlugin(services, pluginContext);
            if (isPluginLoaded)
            {
                // Only load first valid plugin
                break;
            }
        }
    }

    private static bool TryLoadPlugin(IServiceCollection services, PluginContext pluginContext)
    {
        if (pluginContext.Assembly == null)
        {
            return false;
        }

        var readerCCode = pluginContext.CreateExportedInterfaceInstance<IReaderCCode>();
        if (readerCCode == null)
        {
            return false;
        }

        services.AddSingleton(readerCCode);

        var writerCSharpCode = pluginContext.CreateExportedInterfaceInstance<IWriterCSharpCode>();
        if (writerCSharpCode == null)
        {
            return false;
        }

        services.AddSingleton(writerCSharpCode);

        return false;
    }
}
