// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    // NOTE: Properties are required for System.Text.Json serialization
    [PublicAPI]
    public record CRecord : CNode
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("isUnion")]
        public bool IsUnion { get; set; }

        [JsonPropertyName("fields")]
        public ImmutableArray<CRecordField> Fields { get; set; } = ImmutableArray<CRecordField>.Empty;

        [JsonPropertyName("nestedRecords")]
        public ImmutableArray<CRecord> NestedRecords { get; set; } = ImmutableArray<CRecord>.Empty;

        [JsonPropertyName("nestedFunctionPointers")]
        public ImmutableArray<CFunctionPointer> NestedFunctionPointers { get; set; } = ImmutableArray<CFunctionPointer>.Empty;

        public override string ToString()
        {
            var kind= IsUnion ? "Union" : "Struct";
            return $"{kind} {Type} @ {Location.ToString()}";
        }
    }
}
