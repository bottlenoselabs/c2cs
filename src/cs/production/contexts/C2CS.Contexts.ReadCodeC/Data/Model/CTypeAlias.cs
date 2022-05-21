// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Contexts.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CTypeAlias : CNodeWithLocation
{
    [JsonPropertyName("underlying_type")]
    public CTypeInfo UnderlyingTypeInfo { get; set; } = null!;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Typedef '{Name}': {UnderlyingTypeInfo} @ {Location}";
    }
}
