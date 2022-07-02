// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;
using C2CS.Contexts.WriteCodeCSharp.Data;
using JetBrains.Annotations;

namespace C2CS.Data;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
// NOTE: This class must have a unique name across namespaces for usage in System.Text.Json source generators.
[PublicAPI]
public sealed class BindgenConfiguration
{
    [JsonPropertyName("directory")]
    [Json.Schema.Generation.Description("Path of the input and output abstract syntax tree directory. By default, the directory will be used to write a `.json` file for each target platform's abstract syntax tree that has been extracted. By default, the same abstract syntax tree `.json` files will then be read when generating C# code.")]
    public string? InputOutputFileDirectory { get; set; }

    [JsonPropertyName("c")]
    [Json.Schema.Generation.Description("The configuration for reading the `.h` C header file as input of bindgen.")]
    public ReadCodeCConfiguration? ReadCCode { get; set; }

    [JsonPropertyName("cs")]
    [Json.Schema.Generation.Description("The configuration for writing the `.cs` C# source code file as output of bindgen.")]
    public WriteCodeCSharpConfiguration? WriteCSharpCode { get; set; }
}
