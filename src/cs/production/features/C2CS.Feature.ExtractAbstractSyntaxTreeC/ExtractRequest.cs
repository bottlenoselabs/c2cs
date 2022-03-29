// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
// NOTE: This class must have a unique name across namespaces for usage in System.Text.Json source generators.
[PublicAPI]
public sealed class ExtractRequest : UseCaseRequest
{
    /// <summary>
    ///     Path of the input `.h` header file.
    /// </summary>
    [JsonPropertyName("input_file")]
    public string? InputFilePath { get; set; }

    [JsonPropertyName("platforms")]
    public ImmutableDictionary<string, ExtractRequestAbstractSyntaxTree?>? RequestAbstractSyntaxTrees { get; set; }
}
