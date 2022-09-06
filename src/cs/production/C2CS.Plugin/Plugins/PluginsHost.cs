// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.Plugins;

[PublicAPI]
public partial class PluginsHost
{
    private readonly ILogger<PluginsHost> _logger;

    public PluginsHost(ILogger<PluginsHost> logger)
    {
        _logger = logger;
    }

    public ImmutableArray<PluginContext> Plugins { get; private set; } = ImmutableArray<PluginContext>.Empty;

    public bool LoadPlugins(string? pluginsSearchDirectoryPath = null)
    {
        var builder = ImmutableArray.CreateBuilder<PluginContext>();

        var pluginsDirectoryPath = pluginsSearchDirectoryPath ?? Path.Combine(AppContext.BaseDirectory, "plugins");
        if (!Directory.Exists(pluginsDirectoryPath))
        {
            return false;
        }

        var pluginDirectoryPaths = Directory.GetDirectories(pluginsDirectoryPath);

        LogLoadPluginsStart(pluginsDirectoryPath);

        foreach (var pluginDirectoryPath in pluginDirectoryPaths)
        {
            var pluginName = Path.GetFileName(pluginDirectoryPath);
            var pluginAssemblyFilePath = Path.Combine(pluginDirectoryPath, pluginName + ".dll");
            if (!File.Exists(pluginAssemblyFilePath))
            {
                LogLoadPluginFailureFileDoesNotExist(pluginName, pluginAssemblyFilePath);
                continue;
            }

            var plugin = new PluginContext(pluginAssemblyFilePath);
            try
            {
                plugin.Load();
                builder.Add(plugin);
                LogLoadPluginSuccess(pluginName, pluginAssemblyFilePath);
            }
#pragma warning disable CA1031
            catch (Exception e)
#pragma warning restore CA1031
            {
                // log and swallow exception
                // the failure to load a plugin is not critical enough for the program to stop execution
                LogLoadPluginFailure(e, pluginName, pluginsDirectoryPath);
            }
        }

        Plugins = builder.ToImmutable();
        LogLoadPluginsFinish(pluginsDirectoryPath, Plugins.Length);
        return Plugins.Length > 0;
    }

    [LoggerMessage(0, LogLevel.Information, "- Loading plugins from directory: {PluginsDirectoryPath}")]
    private partial void LogLoadPluginsStart(string pluginsDirectoryPath);

    [LoggerMessage(1, LogLevel.Error, "- Failed to load plugin '{PluginName}', the file does not exist: {AssemblyFilePath}")]
    private partial void LogLoadPluginFailureFileDoesNotExist(string pluginName, string assemblyFilePath);

    [LoggerMessage(2, LogLevel.Error, "- Failed to load plugin '{PluginName}' from path '{AssemblyFilePath}'")]
    private partial void LogLoadPluginFailure(Exception exception, string pluginName, string assemblyFilePath);

    [LoggerMessage(3, LogLevel.Information, "- Loaded plugin '{PluginName}' from path '{AssemblyFilePath}'")]
    private partial void LogLoadPluginSuccess(string pluginName, string assemblyFilePath);

    [LoggerMessage(4, LogLevel.Information, "- Loaded {PluginsCount} plugin(s) from directory: {PluginsDirectoryPath}")]
    private partial void LogLoadPluginsFinish(string pluginsDirectoryPath, int pluginsCount);
}
