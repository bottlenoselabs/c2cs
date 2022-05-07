// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Contexts.ReadCodeC.Data.Model;
using Microsoft.Extensions.Logging;

namespace C2CS.Contexts.ReadCodeC.Data.Serialization;

public class CJsonSerializer
{
    private readonly ILogger<CJsonSerializer> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly CJsonSerializerContext _context;

    public CJsonSerializer(
        ILogger<CJsonSerializer> logger, IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;

        var serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _context = new CJsonSerializerContext(serializerOptions);
    }

    public CAbstractSyntaxTree Read(string filePath)
    {
        CAbstractSyntaxTree result;

        var fullFilePath = _fileSystem.Path.GetFullPath(filePath);

        try
        {
            var fileContents = _fileSystem.File.ReadAllText(fullFilePath);
            result = JsonSerializer.Deserialize(fileContents, _context.CAbstractSyntaxTree)!;
            FillNames(result);
            _logger.ReadAbstractSyntaxTreeCSuccess(fullFilePath);
        }
        catch (Exception e)
        {
            _logger.ReadAbstractSyntaxTreeCFailure(fullFilePath, e);
            throw;
        }

        return result;
    }

    private void FillNames(CAbstractSyntaxTree abstractSyntaxTree)
    {
        foreach (var keyValuePair in abstractSyntaxTree.MacroObjects)
        {
            keyValuePair.Value.Name = keyValuePair.Key;
        }

        foreach (var keyValuePair in abstractSyntaxTree.Variables)
        {
            keyValuePair.Value.Name = keyValuePair.Key;
        }

        foreach (var keyValuePair in abstractSyntaxTree.Functions)
        {
            keyValuePair.Value.Name = keyValuePair.Key;
        }

        foreach (var keyValuePair in abstractSyntaxTree.Records)
        {
            keyValuePair.Value.Name = keyValuePair.Key;
        }

        foreach (var keyValuePair in abstractSyntaxTree.Enums)
        {
            keyValuePair.Value.Name = keyValuePair.Key;
        }

        foreach (var keyValuePair in abstractSyntaxTree.TypeAliases)
        {
            keyValuePair.Value.Name = keyValuePair.Key;
        }

        foreach (var keyValuePair in abstractSyntaxTree.OpaqueTypes)
        {
            keyValuePair.Value.Name = keyValuePair.Key;
        }

        foreach (var keyValuePair in abstractSyntaxTree.FunctionPointers)
        {
            keyValuePair.Value.Name = keyValuePair.Key;
        }
    }

    public void Write(CAbstractSyntaxTree abstractSyntaxTree, string filePath)
    {
        var fullFilePath = _fileSystem.Path.GetFullPath(filePath);

        var outputDirectory = _fileSystem.Path.GetDirectoryName(fullFilePath)!;
        if (string.IsNullOrEmpty(outputDirectory))
        {
            outputDirectory = AppContext.BaseDirectory;
            fullFilePath = Path.Combine(Environment.CurrentDirectory, fullFilePath);
        }

        try
        {
            if (!_fileSystem.Directory.Exists(outputDirectory))
            {
                _fileSystem.Directory.CreateDirectory(outputDirectory);
            }

            if (_fileSystem.File.Exists(fullFilePath))
            {
                _fileSystem.File.Delete(fullFilePath);
            }

            var fileContents = JsonSerializer.Serialize(abstractSyntaxTree, _context.Options);

            using var fileStream = _fileSystem.File.OpenWrite(fullFilePath);
            using var textWriter = new StreamWriter(fileStream);
            textWriter.Write(fileContents);
            textWriter.Close();
            fileStream.Close();

            _logger.WriteAbstractSyntaxTreeCSuccess(fullFilePath);
        }
        catch (Exception e)
        {
            _logger.WriteAbstractSyntaxTreeCFailure(fullFilePath, e);
            throw;
        }
    }
}
