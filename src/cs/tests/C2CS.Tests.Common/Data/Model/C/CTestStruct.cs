// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Tests.Common.Data.Model.C;

[PublicAPI]
public class CTestStruct
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("parent_name")]
    public string ParentName { get; set; } = string.Empty;

    [JsonPropertyName("size_of")]
    public int SizeOf { get; set; }

    [JsonPropertyName("align_of")]
    public int AlignOf { get; set; }

    [JsonPropertyName("fields")]
    public ImmutableArray<CTestStructField> Fields { get; set; } = ImmutableArray<CTestStructField>.Empty;
}
