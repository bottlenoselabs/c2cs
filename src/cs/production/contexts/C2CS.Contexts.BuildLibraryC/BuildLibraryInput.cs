// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Contexts.BuildLibraryC.Data;
using C2CS.Foundation;
using JsonSerializerContext = C2CS.Contexts.BuildLibraryC.Data.Serialization.JsonSerializerContext;

namespace C2CS.Contexts.BuildLibraryC;

public class BuildLibraryInput
{
    public BuildProject Project { get; }

    public BuildLibraryInput(BuildProject project)
    {
        Project = project;
    }

    public static BuildLibraryInput GetFrom(IReadOnlyList<string>? args)
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

            var serializerContext = new JsonSerializerContext(jsonSerializerOptions);
            var buildProject = JsonSerializer.Deserialize(fileContents, serializerContext.BuildProject)!;
            var input = new BuildLibraryInput(buildProject);
            return input;
        }
        catch (Exception e)
        {
            throw new ConfigurationException("Failed to read configuration.", e);
        }
    }
}
