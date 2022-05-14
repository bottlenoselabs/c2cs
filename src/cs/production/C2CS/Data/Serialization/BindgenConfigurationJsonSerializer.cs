// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Contexts.WriteCodeCSharp.Data;
using C2CS.Foundation.Data.Serialization;
using Microsoft.Extensions.Logging;

namespace C2CS.Data.Serialization;

public sealed partial class BindgenConfigurationJsonSerializer
{
    private readonly ILogger<BindgenConfigurationJsonSerializer> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public BindgenConfigurationJsonSerializer(
        ILogger<BindgenConfigurationJsonSerializer> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;

        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            Converters =
            {
                new JsonStringEnumConverter(SnakeCaseNamingPolicy.Instance)
            }
        };
    }

    public BindgenConfiguration Read(string filePath)
    {
        var fullFilePath = _fileSystem.Path.GetFullPath(filePath);

        try
        {
            var fileContents = _fileSystem.File.ReadAllText(fullFilePath);
            var configuration = JsonSerializer.Deserialize<BindgenConfiguration>(fileContents, _jsonSerializerOptions)!;

            Polyfill(fullFilePath, configuration);

            LogLoadSuccess(fullFilePath);
            return configuration;
        }
        catch (Exception e)
        {
            LogLoadFailure(fullFilePath, e);
            throw;
        }
    }

    private void Polyfill(string filePath, BindgenConfiguration configuration)
    {
        var configurationReadC = configuration.ReadCCode;
        if (configurationReadC != null)
        {
            PolyfillConfigurationReadC(filePath, configuration, configurationReadC);
        }

        var configurationWriteCSharp = configuration.WriteCSharpCode;
        if (configurationWriteCSharp != null)
        {
            PolyfillConfigurationWriteCSharp(filePath, configuration, configurationWriteCSharp);
        }
    }

    private void PolyfillConfigurationReadC(
        string filePath, BindgenConfiguration configuration, ReadCodeCConfiguration read)
    {
        if (string.IsNullOrEmpty(read.OutputFileDirectory))
        {
            read.OutputFileDirectory = configuration.InputOutputFileDirectory;
        }

        if (string.IsNullOrEmpty(read.WorkingDirectory))
        {
            read.WorkingDirectory = _fileSystem.Path.GetDirectoryName(filePath);
        }
    }

    private void PolyfillConfigurationWriteCSharp(string filePath, BindgenConfiguration configuration, WriteCodeCSharpConfiguration write)
    {
        if (string.IsNullOrEmpty(write.InputFileDirectory))
        {
            write.InputFileDirectory = configuration.InputOutputFileDirectory;
        }

        if (string.IsNullOrEmpty(write.WorkingDirectory))
        {
            write.WorkingDirectory = _fileSystem.Path.GetDirectoryName(filePath);
        }
    }

    [LoggerMessage(0, LogLevel.Information, "Configuration load: Success. Path: {FilePath}.")]
    private partial void LogLoadSuccess(string filePath);

    [LoggerMessage(1, LogLevel.Information, "Configuration load. Failed. Path: {FilePath}.")]
    private partial void LogLoadFailure(string filePath, Exception exception);
}
