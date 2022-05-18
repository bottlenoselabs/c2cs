// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Contexts.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public record CEnumConstant : CNodeWithLocation
{
    [JsonPropertyName("name")]
    public new string Name
    {
        get => base.Name;
        set => base.Name = value;
    }

    [JsonPropertyName("type")]
    public CTypeInfo Type { get; set; } = null!;

    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return $"EnumValue '{Name}' = {Value}";
    }
}
