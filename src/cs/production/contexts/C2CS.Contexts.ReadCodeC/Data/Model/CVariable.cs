// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Contexts.ReadCodeC.Data.Model;

public record CVariable : CNodeWithLocation
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Variable '{Name}': {Type} @ {Location}";
    }
}
