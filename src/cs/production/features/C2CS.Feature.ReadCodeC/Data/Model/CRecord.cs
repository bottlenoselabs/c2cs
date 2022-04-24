// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Feature.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CRecord : CNodeWithLocation
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("parent_name")]
    public string ParentName { get; set; } = string.Empty;

    [JsonPropertyName("record_kind")]
    public CRecordKind RecordKind { get; set; }

    [JsonPropertyName("size_of")]
    public int SizeOf { get; set; }

    [JsonPropertyName("align_of")]
    public int AlignOf { get; set; }

    [JsonPropertyName("fields")]
    public ImmutableArray<CRecordField> Fields { get; set; } = ImmutableArray<CRecordField>.Empty;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"{RecordKind} {Name} @ {Location}";
    }
}