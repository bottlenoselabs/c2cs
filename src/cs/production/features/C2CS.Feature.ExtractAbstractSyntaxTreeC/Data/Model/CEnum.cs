// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
[PublicAPI]
public record CEnum : CNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("typeInteger")]
    public string IntegerType { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public ImmutableArray<CEnumValue> Values { get; set; } = ImmutableArray<CEnumValue>.Empty;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Enum '{Type}': {IntegerType} @ {Location.ToString()}";
    }
}
