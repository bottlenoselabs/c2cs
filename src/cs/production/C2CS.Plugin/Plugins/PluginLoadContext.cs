// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Reflection;
using System.Runtime.Loader;

namespace C2CS.Plugins;

internal class PluginLoadContext : AssemblyLoadContext
{
    // the resolver helps us find dependencies
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string assemblyFilePath)
    {
        _resolver = new AssemblyDependencyResolver(assemblyFilePath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyFilePath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyFilePath == null)
        {
            return null; // perhaps not found, or error resolving a dependency, or could be a framework .dll
        }

        return LoadFromAssemblyPath(assemblyFilePath);
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath == null)
        {
            return IntPtr.Zero; // perhaps not found, or error resolving a dependency
        }

        return LoadUnmanagedDllFromPath(libraryPath);
    }
}
