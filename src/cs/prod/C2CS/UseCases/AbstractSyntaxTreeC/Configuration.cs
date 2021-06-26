// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    // NOTE: Properties are required for System.Text.Json serialization
    [PublicAPI]
    public class Configuration
    {
        [JsonPropertyName("autoFindSDK")]
        public bool AutomaticallyFindSoftwareDevelopmentKit { get; set; } = true;

        [JsonPropertyName("defines")]
        public ImmutableArray<string> Defines { get; set; } = ImmutableArray<string>.Empty;

        [JsonPropertyName("clangArguments")]
        public ImmutableArray<string> ClangArguments { get; set; } = ImmutableArray<string>.Empty;
    }
}
