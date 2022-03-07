// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Feature.BindgenCSharp;
using C2CS.Feature.ExtractAbstractSyntaxTreeC;

namespace C2CS;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
// NOTE: This class must have a unique name across namespaces for usage in System.Text.Json source generators.
public class Configuration
{
    /// <summary>
    ///     The configuration for extracting the C abstract syntax tree from a header file.
    /// </summary>
    [JsonPropertyName("ast")]
    public ConfigurationExtractAbstractSyntaxTreeC? ExtractAbstractSyntaxTreeC { get; set; }

    /// <summary>
    ///     The configuration for generating C# code.
    /// </summary>
    [JsonPropertyName("cs")]
    public ConfigurationBindgenCSharp? BindgenCSharp { get; set; }

    public static Configuration LoadFrom(string filePath)
    {
        try
        {
            var fileContents = File.ReadAllText(filePath);
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,

                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
            var serializerContext = new ConfigurationSerializerContext(jsonSerializerOptions);
            var configuration = JsonSerializer.Deserialize(fileContents, serializerContext.Configuration)!;
            return configuration;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
