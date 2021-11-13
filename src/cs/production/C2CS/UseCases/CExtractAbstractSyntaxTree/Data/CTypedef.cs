// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.UseCases.CExtractAbstractSyntaxTree;

// NOTE: Properties are required for System.Text.Json serialization
[PublicAPI]
public record CTypedef : CNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("underlyingType")]
    public string UnderlyingType { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"Record '{Name}': {UnderlyingType} @ {Location.ToString()}";
    }
}
