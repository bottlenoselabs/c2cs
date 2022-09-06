// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Reflection;
using JetBrains.Annotations;

namespace C2CS.Plugins;

/// <summary>
///     A C# plugin with it's own isolated context for resolving, loading, and unloading dependencies.
/// </summary>
[PublicAPI]
public class PluginContext
{
    private readonly string _assemblyFilePath;
    private readonly PluginLoadContext _loadContext;

    /// <summary>
    ///     Initializes a new instance of the <see cref="PluginContext" /> class.
    /// </summary>
    /// <param name="assemblyFilePath">The file path of the .NET assembly.</param>
    public PluginContext(string assemblyFilePath)
    {
        _assemblyFilePath = assemblyFilePath;
        _loadContext = new PluginLoadContext(_assemblyFilePath);
        Name = Path.GetFileNameWithoutExtension(assemblyFilePath);
    }

    public string Name { get; private set; }

    /// <summary>
    ///     Gets the <see cref="Assembly" /> of the plugin.
    /// </summary>
    /// <remarks>
    ///     <para>If the plugin is not loaded, <see cref="Assembly" /> is <c>null</c>.</para>
    ///     <para>To load the plugin call <see cref="Load" />.</para>
    /// </remarks>
    public Assembly? Assembly { get; private set; }

    /// <summary>
    ///     Gets a value indicating whether this plugin has loaded.
    /// </summary>
    /// <returns><c>true</c> if the plugin is loaded; otherwise, false.</returns>
    public bool IsLoaded => Assembly != null;

    /// <summary>
    ///     Loads the plugin.
    /// </summary>
    public void Load()
    {
        Assembly = _loadContext.LoadFromAssemblyPath(_assemblyFilePath);
    }

    /// <summary>
    ///     Unloads the plugin.
    /// </summary>
    public void Unload()
    {
        _loadContext.Unload();
        Assembly = null;
    }

    public T? CreateExportedInterfaceInstance<T>()
    {
        if (Assembly == null)
        {
            return default;
        }

        var exportedTypes = Assembly.GetExportedTypes();
        var type = exportedTypes.SingleOrDefault(static t =>
        {
            var interfaceName = typeof(T).Name;
            return t.GetInterface(interfaceName) != null;
        });
        if (type == default)
        {
            return default;
        }

        var instanceObject = Activator.CreateInstance(type);
        if (instanceObject is not T pluginInstance)
        {
            // This can happen if T is not shared in the same assembly context between host and the plugin even if they are the same type.
            return default;
        }

        return pluginInstance;
    }

    public ImmutableArray<T> CreateExportedInterfaceInstances<T>()
    {
        if (Assembly == null)
        {
            return ImmutableArray<T>.Empty;
        }

        var builder = ImmutableArray.CreateBuilder<T>();
        var types = Assembly.GetExportedTypes()
            .Where(static t =>
            {
                var interfaceName = typeof(T).Name;
                return t.GetInterface(interfaceName) != null;
            }).ToImmutableArray();

        foreach (var type in types)
        {
            var instanceObject = Activator.CreateInstance(type);
            if (instanceObject is not T pluginInstance)
            {
                // This can happen if T is not shared in the same assembly context between host and the plugin even if they are the same type.
                continue;
            }

            builder.Add(pluginInstance);
        }

        var result = builder.ToImmutable();
        return result;
    }
}
