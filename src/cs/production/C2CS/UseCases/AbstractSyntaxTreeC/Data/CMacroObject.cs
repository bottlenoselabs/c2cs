// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace C2CS.UseCases.AbstractSyntaxTreeC;

public record CMacroObject : CNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("tokens")]
    public ImmutableArray<string> Tokens { get; set; } = ImmutableArray<string>.Empty;

    public override string ToString()
    {
        return $"MacroObject '{Name}' @ {Location.ToString()}";
    }
}