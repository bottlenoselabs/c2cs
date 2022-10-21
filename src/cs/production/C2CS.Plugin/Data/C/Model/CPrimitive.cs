// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;

namespace C2CS.Data.C.Model;

public record CPrimitive : CNode
{
    [JsonPropertyName("type")]
    public CTypeInfo TypeInfo { get; set; } = null!;
}
