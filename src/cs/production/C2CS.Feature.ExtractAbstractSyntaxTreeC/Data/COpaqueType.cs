// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;

public record COpaqueType : CNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"OpaqueType '{Name}' @ {Location.ToString()}";
    }
}
