// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Contexts.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CRecordField : CNodeWithLocation
{
    [JsonPropertyName("name")]
    public new string Name
    {
        get => base.Name;
        set => base.Name = value;
    }

    [JsonPropertyName("type")]
    public CTypeInfo TypeInfo { get; set; } = null!;

    [JsonPropertyName("offset_of")]
    public int? OffsetOf { get; set; }

    [JsonPropertyName("padding_of")]
    public int? PaddingOf { get; set; }

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Record field '{Name}': {TypeInfo} @ {Location}";
    }
}
