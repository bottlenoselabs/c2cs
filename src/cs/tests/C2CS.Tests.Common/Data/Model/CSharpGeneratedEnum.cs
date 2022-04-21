// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Tests.Common.Data.Model;

[PublicAPI]
public class CSharpGeneratedEnum
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("base_type")]
    public string BaseType { get; set; } = string.Empty;

    [JsonPropertyName("members")]
    public ImmutableArray<CSharpGeneratedEnumMember> Members { get; set; } = ImmutableArray<CSharpGeneratedEnumMember>.Empty;
}
