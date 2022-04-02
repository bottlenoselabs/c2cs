// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Feature.ReadCodeC.Data;
using C2CS.Feature.WriteCodeCSharp.Data;
using Microsoft.Extensions.Logging;

namespace C2CS.Data.Serialization;

public sealed class ConfigurationJsonSerializer
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ConfigurationSerializerContext _serializerContext;

    public ConfigurationJsonSerializer(ILogger logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;

        var jsonSerializerOptions = new JsonSerializerOptions
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
        _serializerContext = new ConfigurationSerializerContext(jsonSerializerOptions);
    }

    public Configuration Read(string filePath)
    {
        var fullFilePath = _fileSystem.Path.GetFullPath(filePath);

        try
        {
            var fileContents = _fileSystem.File.ReadAllText(fullFilePath);
            var configuration = JsonSerializer.Deserialize(fileContents, _serializerContext.Configuration)!;

            Polyfill(configuration);

            _logger.ConfigurationLoadSuccess(fullFilePath);
            return configuration;
        }
        catch (Exception e)
        {
            _logger.ConfigurationLoadFailure(fullFilePath, e);
            throw;
        }
    }

    private static void Polyfill(Configuration configuration)
    {
        var requestExtractC = configuration.ReadC;
        if (requestExtractC?.ConfigurationAbstractSyntaxTrees != null)
        {
            foreach (var (_, extractAbstractSyntaxTreeC) in requestExtractC.ConfigurationAbstractSyntaxTrees)
            {
                if (extractAbstractSyntaxTreeC != null)
                {
                    PolyfillExtractAbstractSyntaxTreeC(configuration, extractAbstractSyntaxTreeC);
                }
            }
        }

        var requestBindgenCSharp = configuration.WriteCSharp;
        if (requestBindgenCSharp != null)
        {
            PolyfillBindgenCSharp(configuration, requestBindgenCSharp);
        }
    }

    private static void PolyfillExtractAbstractSyntaxTreeC(
        Configuration configuration, ReadCodeCConfigurationAbstractSyntaxTree extract)
    {
        if (string.IsNullOrEmpty(extract.OutputFileDirectory))
        {
            extract.OutputFileDirectory = configuration.InputOutputFileDirectory;
        }
    }

    private static void PolyfillBindgenCSharp(Configuration configuration, WriteCodeCSharpConfiguration write)
    {
        if (string.IsNullOrEmpty(write.InputFileDirectory))
        {
            write.InputFileDirectory = configuration.InputOutputFileDirectory;
        }
    }
}
