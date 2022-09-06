// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using JetBrains.Annotations;

namespace C2CS.Configuration;

// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
[PublicAPI]
public sealed class ConfigurationBindgen
{
    /// <summary>
    ///     The path of the input and output abstract syntax tree directory.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         By default, the directory will be used to write a `.json` file for each target platform's abstract
    ///         syntax tree that has been extracted. By default, the same abstract syntax tree `.json` files will then
    ///         be read when generating C# code.
    ///     </para>
    /// </remarks>
    public string? InputOutputFileDirectory { get; set; }

    /// <summary>
    ///     The configuration for reading the `.h` C header file as input of bindgen.
    /// </summary>
    public ConfigurationReadCodeC ReadCCode { get; set; } = new();

    /// <summary>
    ///     The configuration for writing the `.cs` C# source code file as output of bindgen.
    /// </summary>
    public ConfigurationWriteCodeCSharp WriteCSharpCode { get; set; } = new();
}
