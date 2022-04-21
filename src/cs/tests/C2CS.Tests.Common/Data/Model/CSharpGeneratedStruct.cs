// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Tests.Common.Data.Model;

[PublicAPI]
public class CSharpGeneratedStruct
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("layout")]
    public CSharpGeneratedStructLayout Layout { get; set; } = null!;

    [JsonPropertyName("fields")]
    public ImmutableArray<CSharpGeneratedStructField> Fields { get; set; } = ImmutableArray<CSharpGeneratedStructField>.Empty;
}
