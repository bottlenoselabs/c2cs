// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using System.IO.Abstractions;
using System.Reflection;
using C2CS.Data.Serialization;
using C2CS.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace C2CS;

public static class Startup
{
    public static IHost CreateHost(string[] args)
    {
        return new HostBuilder()
            .UseConsoleLifetime()
            .BuildHostCommon(args)
            .Build();
    }

    public static IHostBuilder BuildHostCommon(this IHostBuilder builder, string[]? args = null)
    {
        return builder
            .ConfigureServices(services => ConfigureServices(services, args))
            .UseServiceProviderFactory(new DefaultServiceProviderFactory(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            }));
    }

    private static void ConfigureServices(IServiceCollection services, string[]? args)
    {
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton(new CommandLineArgumentsProvider(args ?? Environment.GetCommandLineArgs()));
        services.AddLogging(x =>
            x.AddSimpleConsole(options =>
            {
                options.ColorBehavior = LoggerColorBehavior.Enabled;
                options.SingleLine = true;
                options.IncludeScopes = true;
                options.UseUtcTimestamp = true;
                options.TimestampFormat = "yyyy-dd-MM HH:mm:ss ";
            }));
        services.AddSingleton(x =>
            x.GetRequiredService<ILoggerProvider>().CreateLogger(string.Empty));
        services.AddHostedService<CommandLineService>();
        services.AddSingleton<RootCommand, CommandLineInterface>();
        services.AddSingleton<ConfigurationJsonSerializer>();

        Feature.ReadCodeC.Startup.ConfigureServices(services);
        Feature.WriteCodeCSharp.Startup.ConfigureServices(services);
        Feature.BuildLibraryC.Startup.ConfigureServices(services);
    }
}
