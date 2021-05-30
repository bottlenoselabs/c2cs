// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.CSharp;
using C2CS.Languages.C;

namespace C2CS.UseCases.Bindgen
{
    public class BindgenConfiguration
    {
		[JsonPropertyName("inputFilePath")]
		public string InputFilePath { get; set; } = null!;

		[JsonPropertyName("outputFilePath")]
		public string OutputFilePath { get; set; } = null!;

		[JsonPropertyName("parseC")]
		public CPreferencesParse CPreferencesParse { get; set; } = new();

		[JsonPropertyName("exploreC")]
		public CPreferencesExplore CPreferencesExplore { get; set; } = new();

		[JsonPropertyName("generateCSharp")]
		public CSharpPreferencesGenerate CSharpPreferencesGenerate { get; set; } = new();

		public static BindgenConfiguration LoadFromJson(string filePath)
		{
			var configFilePathContents = File.ReadAllText(filePath);
			var config = JsonSerializer.Deserialize<BindgenConfiguration>(configFilePathContents)!;
			Validate(config);
			FillDefaults(config);
			return config;
		}

		private static void Validate(BindgenConfiguration config)
		{
			if (string.IsNullOrEmpty(config.InputFilePath))
			{
				throw new BindgenConfigurationException("An input .h file was not provided.");
			}

			if (!File.Exists(config.InputFilePath))
			{
				throw new BindgenConfigurationException($"The input .h file '{config.InputFilePath}' does not exist.");
			}
		}

		private static void FillDefaults(BindgenConfiguration config)
		{
			config.InputFilePath = Path.GetFullPath(config.InputFilePath);
			config.OutputFilePath = Path.GetFullPath(config.OutputFilePath);

			var fileName = Path.GetFileName(config.InputFilePath);
			var defaultName = Path.GetFileNameWithoutExtension(fileName);

			if (string.IsNullOrEmpty(config.OutputFilePath))
			{
				config.OutputFilePath = Path.Combine(Environment.CurrentDirectory, $"{defaultName}.cs");
			}

			if (string.IsNullOrEmpty(config.CSharpPreferencesGenerate.ClassName))
			{
				config.CSharpPreferencesGenerate.ClassName = defaultName;
			}

			if (string.IsNullOrEmpty(config.CSharpPreferencesGenerate.LibraryFileName))
			{
				config.CSharpPreferencesGenerate.LibraryFileName = defaultName;
			}
		}
    }
}
