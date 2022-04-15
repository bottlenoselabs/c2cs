// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;
using C2CS.Feature.ReadCodeC.Data.Serialization;

namespace C2CS.Feature.ReadCodeC.Data.Model;

public abstract record CNodeWithLocation : CNode
{
    [JsonPropertyName("location")]
    [JsonConverter(typeof(CLocationJsonConverter))]
    public CLocation Location { get; set; }

    protected override int CompareToInternal(CNode? other)
    {
        if (other is not CNodeWithLocation other2)
        {
            return base.CompareToInternal(other);
        }

        return Location.CompareTo(other2.Location);
    }
}
