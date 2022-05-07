// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Contexts.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CFunctionPointer : CNodeWithLocation
{
    [JsonPropertyName("type")]
    public CTypeInfo TypeInfo { get; set; } = null!;

    [JsonPropertyName("return_type")]
    public CTypeInfo ReturnTypeInfo { get; set; } = null!;

    [JsonPropertyName("parameters")]
    public ImmutableArray<CFunctionPointerParameter> Parameters { get; set; } =
        ImmutableArray<CFunctionPointerParameter>.Empty;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"FunctionPointer {TypeInfo} @ {Location}";
    }
}
