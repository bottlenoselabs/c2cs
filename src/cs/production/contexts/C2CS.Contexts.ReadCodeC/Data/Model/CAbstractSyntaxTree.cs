// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace C2CS.Contexts.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CAbstractSyntaxTree
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("platform_requested")]
    public TargetPlatform PlatformRequested { get; set; } = TargetPlatform.Unknown;

    [JsonPropertyName("platform_actual")]
    public TargetPlatform PlatformActual { get; set; } = TargetPlatform.Unknown;

    [JsonPropertyName("macro_objects")]
    public ImmutableDictionary<string, CMacroObject> MacroObjects { get; set; } = ImmutableDictionary<string, CMacroObject>.Empty;

    [JsonPropertyName("variables")]
    public ImmutableDictionary<string, CVariable> Variables { get; set; } = ImmutableDictionary<string, CVariable>.Empty;

    [JsonPropertyName("functions")]
    public ImmutableDictionary<string, CFunction> Functions { get; set; } = ImmutableDictionary<string, CFunction>.Empty;

    [JsonPropertyName("records")]
    public ImmutableDictionary<string, CRecord> Records { get; set; } = ImmutableDictionary<string, CRecord>.Empty;

    [JsonPropertyName("enums")]
    public ImmutableDictionary<string, CEnum> Enums { get; set; } = ImmutableDictionary<string, CEnum>.Empty;

    [JsonPropertyName("type_aliases")]
    public ImmutableDictionary<string, CTypeAlias> TypeAliases { get; set; } = ImmutableDictionary<string, CTypeAlias>.Empty;

    [JsonPropertyName("opaque_types")]
    public ImmutableDictionary<string, COpaqueType> OpaqueTypes { get; set; } = ImmutableDictionary<string, COpaqueType>.Empty;

    [JsonPropertyName("function_pointers")]
    public ImmutableDictionary<string, CFunctionPointer> FunctionPointers { get; set; } = ImmutableDictionary<string, CFunctionPointer>.Empty;
}
