// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Feature.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CTypedef : CNodeWithLocation
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("underlying_type_name")]
    public string UnderlyingTypeName { get; set; } = string.Empty;

    [JsonPropertyName("underlying_type_kind")]
    public CKind UnderlyingTypeKind { get; set; }

    [JsonPropertyName("underlying_type_size_of")]
    public int UnderlyingTypeSizeOf { get; set; }

    [JsonPropertyName("underlying_type_align_of")]
    public int UnderlyingTypeAlignOf { get; set; }

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Typedef '{Name}': {UnderlyingTypeName} @ {Location}";
    }
}
