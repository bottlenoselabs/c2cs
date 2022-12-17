// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Data.C.Model;

public record COpaqueType : CNodeWithLocation
{
    [JsonPropertyName("size_of")]
    public int SizeOf { get; set; }

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"OpaqueType '{Name}' @ {Location}";
    }
}
