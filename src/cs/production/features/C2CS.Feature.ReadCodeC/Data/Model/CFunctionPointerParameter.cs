// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Feature.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CFunctionPointerParameter : CNodeWithLocation
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"FunctionPointerParameter '{Name}': {Type} @ {Location}";
    }
}
