// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.CommandLine;
using C2CS.Contexts.ReadCodeC;
using C2CS.Contexts.WriteCodeCSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C2CS;

internal partial class CommandLineInterface : RootCommand
{
    private ILogger<CommandLineInterface> _logger;
    private readonly IServiceProvider _serviceProvider;

    private bool _pluginsChecked;

    public CommandLineInterface(
        IServiceProvider serviceProvider,
        ILogger<CommandLineInterface> logger)
        : base("C2CS - C to C# bindings code generator.")
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var abstractSyntaxTreeCommand = new Command(
            "c", "Dump the abstract syntax tree of a C `.h` file to one or more `.json` files per platform.");
        abstractSyntaxTreeCommand.SetHandler(() => HandleReadCodeC());
        AddCommand(abstractSyntaxTreeCommand);

        var bindgenCSharpCommand = new Command(
            "cs",
            "Generate a C# bindings `.cs` file from one or more C abstract syntax tree `.json` files per platform.");
        bindgenCSharpCommand.SetHandler(HandleWriteCSharpCode);
        AddCommand(bindgenCSharpCommand);

        this.SetHandler(Handle);
    }

    private void Handle()
    {
        CheckPlugins();

        var isSuccessReadCodeC = HandleReadCodeC();
        if (isSuccessReadCodeC)
        {
            HandleWriteCSharpCode();
        }
    }

    private bool HandleReadCodeC()
    {
        CheckPlugins();

        var readerCCode = _serviceProvider.GetService<IReaderCCode>();
        if (readerCCode == null)
        {
            LogPluginNotFoundNoReaderCCode();
        }

        var useCase = _serviceProvider.GetService<UseCaseReadCodeC>()!;
        var options = readerCCode!.Options;
        if (options == null)
        {
            return false;
        }

        var response = useCase.Execute(options);
        return response.IsSuccess;
    }

    private void HandleWriteCSharpCode()
    {
        var writerCSharpCode = _serviceProvider.GetService<IWriterCSharpCode>();
        if (writerCSharpCode == null)
        {
            LogPluginNotFoundNoWriterCSharp();
        }

        var useCase = _serviceProvider.GetService<WriteCodeCSharpUseCase>()!;
        var options = writerCSharpCode!.Options;
        if (options == null)
        {
            return;
        }

        useCase.Execute(options);
    }

    private void CheckPlugins()
    {
        if (_pluginsChecked)
        {
            return;
        }

        var pluginHost = Startup.PluginHost;
        if (!pluginHost.Plugins.IsDefaultOrEmpty)
        {
            // TODO: Fallback to .JSON files.
            throw new NotImplementedException();
        }

        foreach (var plugin in pluginHost.Plugins)
        {
            if (plugin.Assembly == null)
            {
                LogInvalidPluginNoAssembly(plugin.Name);
            }
        }

        _pluginsChecked = true;
    }

    [LoggerMessage(0, LogLevel.Error, "- Plugin '{PluginName}' is invalid. There is no loaded Assembly.")]
    private partial void LogInvalidPluginNoAssembly(string pluginName);

    [LoggerMessage(1, LogLevel.Error, "- No plugin was found with a type '" + nameof(IReaderCCode) + "'.")]
    private partial void LogPluginNotFoundNoReaderCCode();

    [LoggerMessage(2, LogLevel.Error, "- No plugin was found with a type '" + nameof(IWriterCSharpCode) + "'.")]
    private partial void LogPluginNotFoundNoWriterCSharp();
}
