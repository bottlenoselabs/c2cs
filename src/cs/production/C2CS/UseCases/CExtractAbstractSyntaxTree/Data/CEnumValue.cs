// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.UseCases.CExtractAbstractSyntaxTree;

// NOTE: Properties are required for System.Text.Json serialization
[PublicAPI]
public record CEnumValue : CNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public long Value { get; set; }

    public override string ToString()
    {
        return $"EnumValue '{Name}' = {Value} @ {Location.ToString()}";
    }
}
