// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS;

[PublicAPI]
public sealed class ReadCodeCConfigurationPlatform
{
    [JsonPropertyName("user_include_directories")]
    [Json.Schema.Generation.Description("The directories to search for non-system header files specific to the target platform.")]
    public ImmutableArray<string?>? UserIncludeDirectories { get; set; }

    [JsonPropertyName("system_include_directories")]
    [Json.Schema.Generation.Description("The directories to search for system header files of the target platform.")]
    public ImmutableArray<string?>? SystemIncludeDirectories { get; set; }

    [JsonPropertyName("defines")]
    [Json.Schema.Generation.Description("Object-like macros to use when parsing C code.")]
    public ImmutableArray<string?>? Defines { get; set; }

    [JsonPropertyName("header_files_blocked")]
    [Json.Schema.Generation.Description("C header file paths to exclude. File paths are relative to the `IncludeDirectories` property.")]
    public ImmutableArray<string?>? HeaderFilesBlocked { get; set; }

    [Json.Schema.Generation.Description("Additional Clang arguments to use when parsing C code.")]
    [JsonPropertyName("clang_arguments")]
    public ImmutableArray<string?>? ClangArguments { get; set; }

    [Json.Schema.Generation.Description("Names of libraries and/or interfaces for macOS, iOS, tvOS or watchOS.")]
    [JsonPropertyName("frameworks")]
    public ImmutableArray<string?>? Frameworks { get; set; }
}
