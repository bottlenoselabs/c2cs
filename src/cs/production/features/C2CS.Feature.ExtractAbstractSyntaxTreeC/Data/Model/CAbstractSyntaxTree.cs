// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
[PublicAPI]
public record CAbstractSyntaxTree
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public RuntimePlatform Platform { get; set; } = RuntimePlatform.linux_x64;

    [JsonPropertyName("functions")]
    public ImmutableArray<CFunction> Functions { get; set; } = ImmutableArray<CFunction>.Empty;

    [JsonPropertyName("functionPointers")]
    public ImmutableArray<CFunctionPointer> FunctionPointers { get; set; } = ImmutableArray<CFunctionPointer>.Empty;

    [JsonPropertyName("records")]
    public ImmutableArray<CRecord> Records { get; set; } = ImmutableArray<CRecord>.Empty;

    [JsonPropertyName("enums")]
    public ImmutableArray<CEnum> Enums { get; set; } = ImmutableArray<CEnum>.Empty;

    [JsonPropertyName("pseudoEnums")]
    public ImmutableArray<CEnum> PseudoEnums { get; set; } = ImmutableArray<CEnum>.Empty;

    [JsonPropertyName("opaqueTypes")]
    public ImmutableArray<COpaqueType> OpaqueTypes { get; set; } = ImmutableArray<COpaqueType>.Empty;

    [JsonPropertyName("typedefs")]
    public ImmutableArray<CTypedef> Typedefs { get; set; } = ImmutableArray<CTypedef>.Empty;

    [JsonPropertyName("variables")]
    public ImmutableArray<CVariable> Variables { get; set; } = ImmutableArray<CVariable>.Empty;

    [JsonPropertyName("types")]
    public ImmutableArray<CType> Types { get; set; } = ImmutableArray<CType>.Empty;

    [JsonPropertyName("constants")]
    public ImmutableArray<CMacroDefinition> Constants { get; set; } = ImmutableArray<CMacroDefinition>.Empty;
}
