// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Contexts.WriteCodeCSharp.Data;
using C2CS.Foundation.Data.Serialization;
using Microsoft.Extensions.Logging;

namespace C2CS.Data.Serialization;

public sealed class BindgenConfigurationJsonSerializer
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

            _logger.ConfigurationLoadSuccess(fullFilePath);
            return configuration;
        }
        catch (Exception e)
        {
            _logger.ConfigurationLoadFailure(fullFilePath, e);
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

        if (read.ConfigurationPlatforms != null)
        {
			foreach (var (_, platform) in read.ConfigurationPlatforms)
            {
                if (platform == null)
                {
                    continue;
                }

                PolyfillConfigurationReadCPlatform(read, platform);
            }
        }
    }

    private static void PolyfillConfigurationReadCPlatform(
        ReadCodeCConfiguration read,
        ReadCodeCConfigurationPlatform platform)
    {
        if (platform.HeaderFilesBlocked == null || platform.HeaderFilesBlocked.Value.IsDefaultOrEmpty)
        {
            platform.HeaderFilesBlocked = read.HeaderFilesBlocked;
        }
        else
        {
            if (read.HeaderFilesBlocked != null && !read.HeaderFilesBlocked.Value.IsDefaultOrEmpty)
            {
                platform.HeaderFilesBlocked = platform.HeaderFilesBlocked.Value.AddRange(read.HeaderFilesBlocked.Value);
            }
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
}
