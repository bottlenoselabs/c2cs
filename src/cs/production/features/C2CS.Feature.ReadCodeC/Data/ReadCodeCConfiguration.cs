// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using C2CS.Foundation.UseCases;
using JetBrains.Annotations;

namespace C2CS.Feature.ReadCodeC.Data;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
// NOTE: This class must have a unique name across namespaces for usage in System.Text.Json source generators.
[PublicAPI]
public sealed class ReadCodeCConfiguration : UseCaseConfiguration
{
    [JsonPropertyName("OutputFileDirectory")]
    [Json.Schema.Generation.Description("Path of the output abstract syntax tree directory. The directory will contain one or more generated abstract syntax tree `.json` files which each have a file name of the target platform.")]
    public string? OutputFileDirectory { get; set; }

    [JsonPropertyName("input_file")]
    [Json.Schema.Generation.Description("Path of the input `.h` header file containing C code.")]
    public string? InputFilePath { get; set; }

    [JsonPropertyName("include_directories")]
    [Json.Schema.Generation.Description("The directories to search for non-system header files.")]
    public ImmutableArray<string?>? IncludeDirectories { get; set; }

    [JsonPropertyName("is_enabled_location_full_paths")]
    [Json.Schema.Generation.Description("Determines whether to show the the path of header code locations with full paths or relative paths. Use `true` to use the full path for header locations. Use `false` or `null` or omit this property to show only relative file paths.")]
    public bool? IsEnabledLocationFullPaths { get; set; }

    [JsonPropertyName("platforms")]
    [Json.Schema.Generation.Description("The target platform configurations for extracting the abstract syntax trees. Each target platform is a Clang target triple. See the C2CS docs for more details about what target platforms are available.")]
#pragma warning disable CA2227
    public Dictionary<string, ReadCodeCConfigurationPlatform?>? ConfigurationPlatforms { get; set; }
#pragma warning restore CA2227
}
