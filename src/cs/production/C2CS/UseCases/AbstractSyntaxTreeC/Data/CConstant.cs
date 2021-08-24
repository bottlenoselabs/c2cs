// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    public record CConstant : CNode
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Value { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"Constant '{Name}': {Value} @ {Location.ToString()}";
        }
    }
}
