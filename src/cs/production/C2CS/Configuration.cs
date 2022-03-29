// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using C2CS.Feature.BindgenCSharp;
using C2CS.Feature.ExtractAbstractSyntaxTreeC;

namespace C2CS;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
// NOTE: This class must have a unique name across namespaces for usage in System.Text.Json source generators.
public sealed class Configuration
{
    /// <summary>
    ///     Path of the input and output abstract syntax tree directory. The directory will be written to with a `.json`
    ///     file for the target platform when extracting the abstract syntax tree for C and the same `.json` files will
    ///     be read when generating C# code.
    /// </summary>
    [JsonPropertyName("directory")]
    public string? InputOutputFileDirectory { get; set; }

    /// <summary>
    ///     The configuration for extracting the C abstract syntax tree(s) from a header file.
    /// </summary>
    [JsonPropertyName("ast")]
    public ExtractRequest? ExtractC { get; set; }

    /// <summary>
    ///     The configuration for generating C# code.
    /// </summary>
    [JsonPropertyName("cs")]
    public BindgenRequest? BindgenCSharp { get; set; }
}
