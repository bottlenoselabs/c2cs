// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace C2CS.Languages.C
{
    public class CPreferencesExplore
    {
        [JsonPropertyName("opaqueTypes")]
        public ImmutableArray<string> OpaqueTypeOverrides { get; set; } = ImmutableArray<string>.Empty;

        [JsonPropertyName("printAbstractSyntaxTree")]
        public bool PrintAbstractSyntaxTree { get; set; } = true;
    }
}
