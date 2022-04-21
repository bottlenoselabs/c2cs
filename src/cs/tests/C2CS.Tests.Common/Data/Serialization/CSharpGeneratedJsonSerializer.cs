// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Tests.Common.Data.Model;
using Xunit;

namespace C2CS.Tests.Common.Data.Serialization;

public class CSharpGeneratedJsonSerializer
{
    private readonly IFileSystem _fileSystem;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public CSharpGeneratedJsonSerializer(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public T? ReadToFile<T>(string filePath)
    {
        var fullFilePath = _fileSystem.Path.GetFullPath(filePath);
        var fileContents = _fileSystem.File.ReadAllText(fullFilePath);
        var result = JsonSerializer.Deserialize<T>(fileContents, _jsonSerializerOptions);
        return result;
    }

    public string WriteToString<T>(T value)
    {
        var fileContents = JsonSerializer.Serialize(value, _jsonSerializerOptions);
        return fileContents;
    }

    public void WriteToFile<T>(string filePath, T value)
    {
        var fullFilePath = _fileSystem.Path.GetFullPath(filePath);

        var outputDirectory = _fileSystem.Path.GetDirectoryName(fullFilePath)!;
        if (string.IsNullOrEmpty(outputDirectory))
        {
            outputDirectory = AppContext.BaseDirectory;
            fullFilePath = Path.Combine(Environment.CurrentDirectory, fullFilePath);
        }

        if (!_fileSystem.Directory.Exists(outputDirectory))
        {
            _fileSystem.Directory.CreateDirectory(outputDirectory);
        }

        if (_fileSystem.File.Exists(fullFilePath))
        {
            _fileSystem.File.Delete(fullFilePath);
        }

        var fileContents = WriteToString(value);

        using var fileStream = _fileSystem.File.OpenWrite(fullFilePath);
        using var textWriter = new StreamWriter(fileStream);
        textWriter.Write(fileContents);
        textWriter.Close();
        fileStream.Close();
    }
}
