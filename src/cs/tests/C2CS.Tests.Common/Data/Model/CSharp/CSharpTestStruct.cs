// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Tests.Common.Data.Model.CSharp;

[PublicAPI]
public class CSharpTestStruct
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("layout")]
    public CSharpTestStructLayout Layout { get; set; } = null!;

    [JsonPropertyName("fields")]
    public ImmutableArray<CSharpTestStructField> Fields { get; set; } = ImmutableArray<CSharpTestStructField>.Empty;

    public override string ToString()
    {
        return Name;
    }
}
