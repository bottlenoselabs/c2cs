// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace C2CS.Tests.Foundation;

public abstract class TestBase
{
    private readonly string _baseDataFilesDirectory;
    private readonly IFileSystem _fileSystem;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly bool _regenerateDataFiles;
    private readonly string _sourceDirectoryPath;

    protected IServiceProvider Services { get; }

    protected TestBase(string baseDataFilesDirectory, bool regenerateDataFiles = false)
    {
        _baseDataFilesDirectory = baseDataFilesDirectory;

        Services = TestHost.Services;

        _fileSystem = Services.GetService<IFileSystem>()!;

        _sourceDirectoryPath = Path.Combine(GetGitDirectory(), "src/cs/tests/C2CS.Tests");
        _regenerateDataFiles = regenerateDataFiles;

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

    protected void AssertValue<T>(string name, T value, string directory)
    {
#pragma warning disable CA1308
        var nameAsWords = name.ToLowerInvariant().Replace("_", " ", StringComparison.InvariantCulture);
#pragma warning restore CA1308
        var nameAsWordsTitleCased = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(nameAsWords);
        var nameNormalized = nameAsWordsTitleCased.Replace(" ", string.Empty, StringComparison.InvariantCulture);
        var relativeJsonFilePath =
            _fileSystem.Path.Combine(_baseDataFilesDirectory, directory, $"{nameNormalized}.json");
        string jsonFilePath;
        if (_regenerateDataFiles)
        {
            jsonFilePath = _fileSystem.Path.Combine(_sourceDirectoryPath, relativeJsonFilePath);
            RegenerateDataFile(jsonFilePath, value);
        }
        else
        {
            jsonFilePath = _fileSystem.Path.Combine(AppContext.BaseDirectory, relativeJsonFilePath);
        }

        var expectedValue = ReadValueFromFile<T>(jsonFilePath);
        value.Should().BeEquivalentTo(
            expectedValue,
            o => o.ComparingByMembers<T>(),
            $"because that is what the JSON file has `{jsonFilePath}`");
    }

    private void RegenerateDataFile<T>(string filePath, T value)
    {
        WriteValueToFile(filePath, value);
    }

    private T? ReadValueFromFile<T>(string filePath)
    {
        var fileContents = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<T>(fileContents, _jsonSerializerOptions);
    }

    private void WriteValueToFile<T>(string filePath, T value)
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

        var fileContents = JsonSerializer.Serialize(value, _jsonSerializerOptions);

        using var fileStream = _fileSystem.File.OpenWrite(fullFilePath);
        using var textWriter = new StreamWriter(fileStream);
        textWriter.Write(fileContents);
        textWriter.Close();
        fileStream.Close();
    }

    private static string GetGitDirectory()
    {
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../src/cs/tests/C2CS.Tests"));

        var currentDirectory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(currentDirectory) && Directory.Exists(currentDirectory))
        {
            var files = Directory.GetFiles(currentDirectory, "*.gitignore");
            if (files.Length == 1)
            {
                return currentDirectory;
            }

            currentDirectory = Directory.GetParent(currentDirectory)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Could not find Git root directory");
    }
}
