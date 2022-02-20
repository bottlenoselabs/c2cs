// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;

public record CMacroDefinition : CNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tokens")]
    public ImmutableArray<string> Tokens { get; set; } = ImmutableArray<string>.Empty;

    public override string ToString()
    {
        return $"Macro '{Name}' @ {Location.ToString()}";
    }
}
