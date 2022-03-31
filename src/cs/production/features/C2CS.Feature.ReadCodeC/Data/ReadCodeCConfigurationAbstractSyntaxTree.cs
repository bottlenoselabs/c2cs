// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Feature.ReadCodeC.Data;

[PublicAPI]
public sealed class ReadCodeCConfigurationAbstractSyntaxTree
{
    [JsonIgnore]
    public string? OutputFileDirectory { get; set; }

    [JsonPropertyName("find_sdk")]
    [Json.Schema.Generation.Description("Determines whether the software development kit (SDK) for C/C++ is attempted to be found. Default is `true`. If `true`, the C/C++ header files for the current operating system are attempted to be found by some reasonable means. If the C/C++ header files can not be found, then an error is generated which halts the program. If `false`, the C/C++ header files will likely be missing causing Clang to generate parsing errors which also halts the program. In such a case, the missing C/C++ header files can be supplied to Clang using the `ClangArguments` property such as \"-isystemPATH/TO/SYSTEM/HEADER/DIRECTORY\"")]
    public bool? IsEnabledFindSdk { get; set; } = true;

    [JsonPropertyName("include")]
    [Json.Schema.Generation.Description("Search directory paths to use for `#include` usages when parsing C code.")]
    public ImmutableArray<string?>? IncludeDirectories { get; set; }

    [JsonPropertyName("defines")]
    [Json.Schema.Generation.Description("Object-like macros to use when parsing C code.")]
    public ImmutableArray<string?>? Defines { get; set; }

    [JsonPropertyName("exclude")]
    [Json.Schema.Generation.Description("C header file names to exclude. File names are relative to the `IncludeDirectories` property.")]
    public ImmutableArray<string?>? ExcludedHeaderFiles { get; set; }

    [JsonPropertyName("function_names")]
    [Json.Schema.Generation.Description("The C function names to explicitly include when parsing C code. Default is `null`. If `null<`, no white list applies. Note that C function names which are excluded also exclude any transitive types.")]
    public ImmutableArray<string?>? FunctionNamesWhiteList { get; set; }

    [JsonPropertyName("opaque_names")]
    [Json.Schema.Generation.Description("Type names that may be found when parsing C code that will be interpreted as opaque types. Opaque types are often used with a pointer to hide the information about the bit layout behind the pointer.")]
    public ImmutableArray<string?>? OpaqueTypeNames { get; set; }

    [Json.Schema.Generation.Description("Additional Clang arguments to use when parsing C code.")]
    [JsonPropertyName("clang_arguments")]
    public ImmutableArray<string?>? ClangArguments { get; set; }
}
