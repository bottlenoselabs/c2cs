// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    // NOTE: Properties are required for System.Text.Json serialization
    [PublicAPI]
    public record CAbstractSyntaxTree
    {
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("functions")]
        public ImmutableArray<CFunction> Functions { get; set; } = ImmutableArray<CFunction>.Empty;

        [JsonPropertyName("functionPointers")]
        public ImmutableArray<CFunctionPointer> FunctionPointers { get; set; } = ImmutableArray<CFunctionPointer>.Empty;

        [JsonPropertyName("records")]
        public ImmutableArray<CRecord> Records { get; set; } = ImmutableArray<CRecord>.Empty;

        [JsonPropertyName("enums")]
        public ImmutableArray<CEnum> Enums { get; set; } = ImmutableArray<CEnum>.Empty;

        [JsonPropertyName("opaqueTypes")]
        public ImmutableArray<COpaqueType> OpaqueTypes { get; set; } = ImmutableArray<COpaqueType>.Empty;

        [JsonPropertyName("typedefs")]
        public ImmutableArray<CTypedef> Typedefs { get; set; } = ImmutableArray<CTypedef>.Empty;

        [JsonPropertyName("variables")]
        public ImmutableArray<CVariable> Variables { get; set; } = ImmutableArray<CVariable>.Empty;

        [JsonPropertyName("types")]
        public ImmutableArray<CType> Types { get; set; } = ImmutableArray<CType>.Empty;
    }
}
