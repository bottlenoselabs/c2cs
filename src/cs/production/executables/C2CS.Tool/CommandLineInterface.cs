// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using C2CS.ReadCodeC;
using C2CS.WriteCodeCSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C2CS;

internal partial class CommandLineInterface : RootCommand
{
    private readonly ILogger<CommandLineInterface> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CommandLineInterface(
        IServiceProvider serviceProvider,
        ILogger<CommandLineInterface> logger)
        : base("C2CS - C to C# bindings code generator.")
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var abstractSyntaxTreeCommand = new Command(
            "c", "Dump the abstract syntax tree of a C `.h` file to one or more `.json` files per platform.");
        abstractSyntaxTreeCommand.SetHandler(HandleReadCCode);
        AddCommand(abstractSyntaxTreeCommand);

        var bindgenCSharpCommand = new Command(
            "cs",
            "Generate a C# bindings `.cs` file from one or more C abstract syntax tree `.json` files per platform.");
        bindgenCSharpCommand.SetHandler(HandleWriteCSharpCode);
        AddCommand(bindgenCSharpCommand);
    }

    private void HandleReadCCode()
    {
        if (!CheckPlugins())
        {
            return;
        }

        var readerCCode = _serviceProvider.GetService<IReaderCCode>();
        if (readerCCode == null)
        {
            LogPluginNotFoundNoReaderCCode();
            return;
        }

        var useCase = _serviceProvider.GetService<UseCaseReadCodeC>()!;
        var options = readerCCode.Options;
        if (options == null)
        {
            return;
        }

        var response = useCase.Execute(options);
    }

    private void HandleWriteCSharpCode()
    {
        if (!CheckPlugins())
        {
            return;
        }

        var writerCSharpCode = _serviceProvider.GetService<IWriterCSharpCode>();
        if (writerCSharpCode == null)
        {
            LogPluginNotFoundNoWriterCSharp();
            return;
        }

        var useCase = _serviceProvider.GetService<WriteCodeCSharpUseCase>()!;
        var options = writerCSharpCode.Options;
        if (options == null)
        {
            return;
        }

        var response = useCase.Execute(options);
    }

    private bool CheckPlugins()
    {
        var pluginHost = Startup.PluginHost;

        if (pluginHost.SearchedFileDirectory == null)
        {
            return false;
        }

        LogSearchedForPlugins(pluginHost.SearchedFileDirectory);

        if (pluginHost.Plugins.IsDefaultOrEmpty)
        {
            LogNoPluginsFound();
            return false;
        }

        foreach (var plugin in pluginHost.Plugins)
        {
            if (plugin.Assembly == null)
            {
                LogInvalidPluginNoAssembly(plugin.Name);
            }

            LogPluginFound(plugin.Name);
        }

        return true;
    }

    [LoggerMessage(0, LogLevel.Information, "- Searched for plugins in file directory: {PluginsSearchFileDirectory}")]
    private partial void LogSearchedForPlugins(string pluginsSearchFileDirectory);

    [LoggerMessage(1, LogLevel.Error, "- No plugins were found; please see https://github.com/bottlenoselabs/c2cs for documentation")]
    private partial void LogNoPluginsFound();

    [LoggerMessage(2, LogLevel.Error, "- Plugin '{PluginName}' is invalid: no loaded assembly")]
    private partial void LogInvalidPluginNoAssembly(string pluginName);

    [LoggerMessage(3, LogLevel.Error, "- No plugin was found with a type '" + nameof(IReaderCCode) + "'")]
    private partial void LogPluginNotFoundNoReaderCCode();

    [LoggerMessage(4, LogLevel.Error, "- No plugin was found with a type '" + nameof(IWriterCSharpCode) + "'")]
    private partial void LogPluginNotFoundNoWriterCSharp();

    [LoggerMessage(5, LogLevel.Information, "- Plugin found: {PluginName}")]
    private partial void LogPluginFound(string pluginName);
}
