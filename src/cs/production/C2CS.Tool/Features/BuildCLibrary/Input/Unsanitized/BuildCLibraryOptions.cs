// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using C2CS.Foundation.Tool;
using JetBrains.Annotations;

namespace C2CS.Features.BuildCLibrary.Input.Unsanitized;

// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
[PublicAPI]
public class BuildCLibraryOptions : ToolUnsanitizedInput
{
    /// <summary>
    ///     The directory path to the CMakeLists.txt file.
    /// </summary>
    [JsonPropertyName("cMakeDirectoryPath")]
    public string? CMakeDirectoryPath { get; set; }

    /// <summary>
    ///     The directory path where to place the built C shared library (`.dll`/`.dylib`/`.so`).
    /// </summary>
    [JsonPropertyName("outputDirectoryPath")]
    public string? OutputDirectoryPath { get; set; }

    /// <summary>
    ///     Additional CMake arguments when generating build files.
    /// </summary>
    [JsonPropertyName("cMakeArguments")]
    public ImmutableArray<string?>? CMakeArguments { get; set; }

    /// <summary>
    ///     Determines whether to delete CMake build files after they are no longer required.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to delete CMake build files after building the C library. Use
    ///         <c>false</c> to keep CMake build files after building the C library.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledDeleteBuildFiles")]
    public bool? IsEnabledDeleteBuildFiles { get; set; }

    /// <summary>
    ///     Determines whether to build the C shared library with debug symbols.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>false</c>. Use <c>true</c> to build the C shared library with debug symbols. Use
    ///         <c>false</c> to build the C shared library without debug symbols.
    ///     </para>
    ///     <para>
    ///         Including debug symbols can be helpful for doing diagnostics with a C shared library such as
    ///         attaching a native debugger or printing extra information to standard out or standard error at runtime.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledDebugBuild")]
    public bool? IsEnabledDebugBuild { get; set; }
}
