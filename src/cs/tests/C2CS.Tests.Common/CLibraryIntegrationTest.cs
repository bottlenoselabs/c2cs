// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.Tests.Common;

public abstract class CLibraryIntegrationTest
{
	private readonly string _libraryName;
	private readonly string _sourceDirectoryPath;
	private readonly string _dataDirectoryPath;
	private readonly bool _regenerateDataFiles;
	private readonly IFileSystem _fileSystem;
	private readonly JsonSerializerOptions _jsonSerializerOptions;

	protected CLibraryIntegrationTest(
		IServiceProvider services,
		string libraryName,
		string dataDirectoryPath,
		bool regenerateDataFiles)
	{
		_libraryName = libraryName;
		_fileSystem = services.GetService<IFileSystem>()!;
		_sourceDirectoryPath = SourceDirectory.Path;
		_dataDirectoryPath = dataDirectoryPath;
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
		string jsonFilePath;
		if (_regenerateDataFiles)
		{
			jsonFilePath = _fileSystem.Path.Combine(
				_sourceDirectoryPath, _libraryName, _dataDirectoryPath, directory, $"{name}.json");
			RegenerateDataFile(jsonFilePath, value);
		}
		else
		{
			jsonFilePath = _fileSystem.Path.Combine(
				AppContext.BaseDirectory, _libraryName, _dataDirectoryPath, directory, $"{name}.json");
		}

		var jsonActual = WriteValueToString(value);
		var jsonExpected = ReadTestFileContents(jsonFilePath);
		var userMessage = jsonFilePath + ": Assert.Equal() Failure";
		AssertX.Equal(jsonExpected, jsonActual, userMessage);
	}

	private void RegenerateDataFile<T>(string filePath, T value)
	{
		WriteValueToFile(filePath, value);
	}

	private string ReadTestFileContents(string filePath)
	{
		return File.ReadAllText(filePath);
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

		var fileContents = WriteValueToString(value);

		using var fileStream = _fileSystem.File.OpenWrite(fullFilePath);
		using var textWriter = new StreamWriter(fileStream);
		textWriter.Write(fileContents);
		textWriter.Close();
		fileStream.Close();
	}

	private string WriteValueToString<T>(T value)
	{
		var fileContents = JsonSerializer.Serialize(value, _jsonSerializerOptions);
		return fileContents;
	}
}
