// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace C2CS.Feature.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CAbstractSyntaxTree
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public TargetPlatform Platform { get; set; } = TargetPlatform.Unknown;

    [JsonPropertyName("functions")]
    public ImmutableDictionary<string, CFunction> Functions { get; set; } = ImmutableDictionary<string, CFunction>.Empty;

    [JsonPropertyName("function_pointers")]
    public ImmutableDictionary<string, CFunctionPointer> FunctionPointers { get; set; } = ImmutableDictionary<string, CFunctionPointer>.Empty;

    [JsonPropertyName("records")]
    public ImmutableDictionary<string, CRecord> Records { get; set; } = ImmutableDictionary<string, CRecord>.Empty;

    [JsonPropertyName("enums")]
    public ImmutableDictionary<string, CEnum> Enums { get; set; } = ImmutableDictionary<string, CEnum>.Empty;

    [JsonPropertyName("opaque_types")]
    public ImmutableDictionary<string, COpaqueType> OpaqueTypes { get; set; } = ImmutableDictionary<string, COpaqueType>.Empty;

    [JsonPropertyName("typedefs")]
    public ImmutableDictionary<string, CTypedef> Typedefs { get; set; } = ImmutableDictionary<string, CTypedef>.Empty;

    [JsonPropertyName("variables")]
    public ImmutableDictionary<string, CVariable> Variables { get; set; } = ImmutableDictionary<string, CVariable>.Empty;

    [JsonPropertyName("types")]
    public ImmutableDictionary<string, CType> Types { get; set; } = ImmutableDictionary<string, CType>.Empty;

    [JsonPropertyName("constants")]
    public ImmutableDictionary<string, CMacroDefinition> Constants { get; set; } = ImmutableDictionary<string, CMacroDefinition>.Empty;
}
