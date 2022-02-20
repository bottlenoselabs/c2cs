// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Feature.BuildLibraryC.Data.Serialization;
using C2CS.Feature.BuildLibraryC.Domain;

namespace C2CS.Feature.BuildLibraryC;

public class Input
{
    public ImmutableArray<BuildTarget> BuildTargets { get; }

    public Input(ImmutableArray<BuildTarget> buildTargets)
    {
        BuildTargets = buildTargets;
    }

    public static Input GetFrom(IReadOnlyList<string>? args)
    {
        var argsCount = args?.Count ?? 0;

        var configurationFilePath = argsCount switch
        {
            1 => args![0],
            _ => throw new ConfigurationException(
                "Unsupported number of arguments for building C library.")
        };

        try
        {
            var fileContents = File.ReadAllText(configurationFilePath);
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

            var serializerContext = new InputDataSerializerContext(jsonSerializerOptions);
            var data = JsonSerializer.Deserialize(fileContents, serializerContext.InputData)!;
            var input = DomainMapper.InputFrom(data);
            return input;
        }
        catch (Exception e)
        {
            throw new ConfigurationException("Failed to read configuration.", e);
        }
    }
}
