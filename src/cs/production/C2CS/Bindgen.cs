// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Contexts.ReadCodeC.Domain;
using C2CS.Contexts.WriteCodeCSharp.Domain;
using C2CS.Data;
using BindgenConfigurationJsonSerializer = C2CS.Data.Serialization.BindgenConfigurationJsonSerializer;

namespace C2CS;

public class Bindgen
{
    private readonly BindgenConfigurationJsonSerializer _configurationJsonSerializer;
    private readonly Contexts.ReadCodeC.UseCase _useCaseReadCodeC;
    private readonly Contexts.WriteCodeCSharp.UseCase _useCaseWriteCodeCSharp;

    public Bindgen(
        BindgenConfigurationJsonSerializer configurationJsonSerializer,
        Contexts.ReadCodeC.UseCase useCaseReadCodeC,
        Contexts.WriteCodeCSharp.UseCase useCaseWriteCodeCSharp)
    {
        _useCaseReadCodeC = useCaseReadCodeC;
        _configurationJsonSerializer = configurationJsonSerializer;
        _useCaseWriteCodeCSharp = useCaseWriteCodeCSharp;
    }

    public bool Execute(string configurationFilePath)
    {
        if (string.IsNullOrEmpty(configurationFilePath))
        {
            configurationFilePath = "config.json";
        }

        var configuration = _configurationJsonSerializer.Read(configurationFilePath);
        return Execute(configuration);
    }

    public bool Execute(BindgenConfiguration configuration)
    {
        var configurationReadC = configuration.ReadCCode;
        var configurationWriteCSharp = configuration.WriteCSharpCode;
        if (configurationReadC == null || configurationWriteCSharp == null)
        {
            return false;
        }

        var resultReadCodeC = _useCaseReadCodeC.Execute(configurationReadC);
        if (!resultReadCodeC.IsSuccess)
        {
            return false;
        }

        var resultWriteCodeCSharp = _useCaseWriteCodeCSharp.Execute(configurationWriteCSharp);
        return resultWriteCodeCSharp.IsSuccess;
    }

    public ReadCodeCOutput? ReadCodeC(string configurationFilePath)
    {
        if (string.IsNullOrEmpty(configurationFilePath))
        {
            configurationFilePath = "config.json";
        }

        var configuration = _configurationJsonSerializer.Read(configurationFilePath);
        var configurationReadC = configuration.ReadCCode;
        if (configurationReadC == null)
        {
            return null;
        }

        return _useCaseReadCodeC.Execute(configurationReadC);
    }

    public WriteCodeCSharpOutput? WriteCodeCSharp(string configurationFilePath)
    {
        if (string.IsNullOrEmpty(configurationFilePath))
        {
            configurationFilePath = "config.json";
        }

        var configuration = _configurationJsonSerializer.Read(configurationFilePath);
        var configurationWriteCSharp = configuration.WriteCSharpCode;
        if (configurationWriteCSharp == null)
        {
            return null;
        }

        return _useCaseWriteCodeCSharp.Execute(configurationWriteCSharp);
    }
}
