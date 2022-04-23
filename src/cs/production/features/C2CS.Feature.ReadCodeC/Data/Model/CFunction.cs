// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Feature.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CFunction : CNodeWithLocation
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("calling_convention")]
    public CFunctionCallingConvention CallingConvention { get; set; } = CFunctionCallingConvention.Cdecl;

    [JsonPropertyName("return_type")]
    public string ReturnType { get; set; } = null!;

    [JsonPropertyName("parameters")]
    public ImmutableArray<CFunctionParameter> Parameters { get; set; } = ImmutableArray<CFunctionParameter>.Empty;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"FunctionExtern '{Name}' @ {Location}";
    }
}
