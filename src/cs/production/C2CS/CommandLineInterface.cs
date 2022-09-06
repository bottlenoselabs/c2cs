// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using System.IO;
using C2CS.Contexts.ReadCodeC;
using C2CS.Contexts.WriteCodeCSharp;
using C2CS.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C2CS;

internal partial class CommandLineInterface : RootCommand
{
    private ILogger<CommandLineInterface> _logger;
    private readonly PluginsHost _pluginsHost;
    private readonly IServiceProvider _serviceProvider;

    private IPluginBindgen? _pluginBindgenConfiguration;

    private bool _pluginsAreLoaded;

    public CommandLineInterface(
        IServiceProvider serviceProvider,
        PluginsHost pluginsHost,
        ILogger<CommandLineInterface> logger)
        : base("C2CS - C to C# bindings code generator.")
    {
        _serviceProvider = serviceProvider;
        _pluginsHost = pluginsHost;
        _logger = logger;

        var pluginsDirectoryPathOption = new Option<string?>(
            new[] { "--plugins", "-p" },
            "File path of the plugins folder.")
        {
            IsRequired = false
        };
        AddGlobalOption(pluginsDirectoryPathOption);

        var abstractSyntaxTreeCommand = new Command(
            "c", "Dump the abstract syntax tree of a C `.h` file to one or more `.json` files per platform.");
        abstractSyntaxTreeCommand.AddOption(pluginsDirectoryPathOption);
        abstractSyntaxTreeCommand.SetHandler<string>(
            pluginsDirectoryPath => HandleReadCodeC(pluginsDirectoryPath),
            pluginsDirectoryPathOption);
        AddCommand(abstractSyntaxTreeCommand);

        var bindgenCSharpCommand = new Command(
            "cs",
            "Generate a C# bindings `.cs` file from one or more C abstract syntax tree `.json` files per platform.");
        bindgenCSharpCommand.AddOption(pluginsDirectoryPathOption);
        bindgenCSharpCommand.SetHandler<string>(HandleWriteCSharpCode, pluginsDirectoryPathOption);
        AddCommand(bindgenCSharpCommand);

        this.SetHandler<string>(Handle, pluginsDirectoryPathOption);
    }

    private void TryLoadPlugins(string? pluginsDirectoryPath)
    {
        if (_pluginsAreLoaded)
        {
            return;
        }

        _pluginsHost.LoadPlugins(pluginsDirectoryPath ?? Path.Combine(Environment.CurrentDirectory, "plugins"));

        foreach (var pluginContext in _pluginsHost.Plugins)
        {
            if (pluginContext.Assembly == null)
            {
                LogInvalidPluginNoAssembly(pluginContext.Name);
                continue;
            }

            var pluginBindgenConfiguration = pluginContext.CreateExportedInterfaceInstance<IPluginBindgen>();
            if (pluginBindgenConfiguration == null)
            {
                LogInvalidPluginNoBindenConfiguration(pluginContext.Name);
                continue;
            }

            _pluginBindgenConfiguration = pluginBindgenConfiguration;
            break;
        }

        _pluginsAreLoaded = true;
    }

    private void Handle(string? pluginsDirectoryPath)
    {
        var isSuccessReadCodeC = HandleReadCodeC(pluginsDirectoryPath);
        if (isSuccessReadCodeC)
        {
            HandleWriteCSharpCode(pluginsDirectoryPath);
        }
    }

    private bool HandleReadCodeC(string? pluginsDirectoryPath)
    {
        TryLoadPlugins(pluginsDirectoryPath);

        var useCase = _serviceProvider.GetService<ReadCodeCUseCase>()!;
        var configuration = _pluginBindgenConfiguration?.Configuration.ReadCCode;
        if (configuration == null)
        {
            return false;
        }

        var response = useCase.Execute(configuration);
        return response.IsSuccess;
    }

    private void HandleWriteCSharpCode(string? pluginsDirectoryPath)
    {
        TryLoadPlugins(pluginsDirectoryPath);

        var useCase = _serviceProvider.GetService<WriteCodeCSharpUseCase>()!;
        var configuration = _pluginBindgenConfiguration?.Configuration.WriteCSharpCode;
        if (configuration == null)
        {
            return;
        }

        useCase.Execute(configuration);
    }

    [LoggerMessage(0, LogLevel.Error, "- Plugin '{PluginName}' is invalid. There is no loaded Assembly.")]
    private partial void LogInvalidPluginNoAssembly(string pluginName);

    [LoggerMessage(1, LogLevel.Error, "- Plugin '{PluginName}' is invalid. Could not find the type 'IPluginBindgenConfiguration'.")]
    private partial void LogInvalidPluginNoBindenConfiguration(string pluginName);
}
