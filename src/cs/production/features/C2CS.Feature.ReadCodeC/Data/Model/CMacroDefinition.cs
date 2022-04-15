// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Feature.ReadCodeC.Data.Model;

public record CMacroDefinition : CNodeWithLocation
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tokens")]
    public ImmutableArray<string> Tokens { get; set; } = ImmutableArray<string>.Empty;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Macro '{Name}' @ {Location}";
    }
}
