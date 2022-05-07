// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Contexts.ReadCodeC.Data.Model;

public record CMacroObject : CNodeWithLocation
{
    [JsonPropertyName("tokens")]
    public ImmutableArray<string> Tokens { get; set; } = ImmutableArray<string>.Empty;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"Macro '{Name}' @ {Location}";
    }
}
