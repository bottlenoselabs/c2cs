// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Tests.Common.Data.Model.C;

[PublicAPI]
public class CTestRecord
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("size_of")]
    public int SizeOf { get; set; }

    [JsonPropertyName("align_of")]
    public int AlignOf { get; set; }

    [JsonPropertyName("is_union")]
    public bool IsUnion { get; set; }

    [JsonIgnore]
    public bool IsStruct => !IsUnion;

    [JsonPropertyName("fields")]
    public ImmutableArray<CTestRecordField> Fields { get; set; } = ImmutableArray<CTestRecordField>.Empty;

    public override string ToString()
    {
        return Name;
    }
}
