// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Feature.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CStructField : CNodeWithLocation
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("offset_of")]
    public int OffsetOf { get; set; }

    [JsonPropertyName("padding_of")]
    public int PaddingOf { get; set; }

    [JsonPropertyName("size_of")]
    public int SizeOf { get; set; }

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Struct Field '{Name}': {Type} @ {Location}";
    }
}
