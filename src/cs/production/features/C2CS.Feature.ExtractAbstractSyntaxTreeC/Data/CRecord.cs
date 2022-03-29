// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;

// NOTE: Properties are required for System.Text.Json serialization
public record CRecord : CNode
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("isUnion")]
    public bool IsUnion { get; set; }

    [JsonPropertyName("fields")]
    public ImmutableArray<CRecordField> Fields { get; set; } = ImmutableArray<CRecordField>.Empty;

    [JsonPropertyName("nestedRecords")]
    public ImmutableArray<CRecord> NestedRecords { get; set; } = ImmutableArray<CRecord>.Empty;

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        var kind = IsUnion ? "Union" : "Struct";
        return $"{kind} {Name} @ {Location.ToString()}";
    }
}
