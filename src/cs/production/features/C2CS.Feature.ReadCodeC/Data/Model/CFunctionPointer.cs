// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Feature.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CFunctionPointer : CNodeWithLocation
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public CType Type { get; set; } = null!;

    [JsonPropertyName("return_type")]
    public CType ReturnType { get; set; } = null!;

    [JsonPropertyName("parameters")]
    public ImmutableArray<CFunctionPointerParameter> Parameters { get; set; } =
        ImmutableArray<CFunctionPointerParameter>.Empty;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"FunctionPointer {Type} @ {Location}";
    }
}
