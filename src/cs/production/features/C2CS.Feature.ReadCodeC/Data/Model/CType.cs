// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using C2CS.Feature.ReadCodeC.Data.Serialization;

namespace C2CS.Feature.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public class CType
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public CKind Kind { get; set; } = CKind.Unknown;

    [JsonPropertyName("sizeOf")]
    public int SizeOf { get; set; }

    [JsonPropertyName("alignOf")]
    public int? AlignOf { get; set; }

    [JsonPropertyName("sizeOfElement")]
    public int? ElementSize { get; set; }

    [JsonPropertyName("arraySize")]
    public int? ArraySize { get; set; }

    [JsonPropertyName("location")]
    [JsonConverter(typeof(CLocationJsonConverter))]
    public CLocation Location { get; set; }

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return Name;
    }
}
