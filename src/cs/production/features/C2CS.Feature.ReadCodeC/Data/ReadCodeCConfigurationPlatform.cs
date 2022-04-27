// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Feature.ReadCodeC.Data;

[PublicAPI]
public sealed class ReadCodeCConfigurationPlatform
{
    [JsonIgnore]
    public string? OutputFileDirectory { get; set; }

    [JsonPropertyName("system_include_directories")]
    [Json.Schema.Generation.Description("The directories to search for system header files of the target platform.")]
    public ImmutableArray<string?>? SystemIncludeDirectories { get; set; }

    [JsonPropertyName("include_directories")]
    [Json.Schema.Generation.Description("The directories to search for non-system header files specific to the target platform.")]
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

    [JsonPropertyName("is_enabled_location_full_paths")]
    [Json.Schema.Generation.Description("Determines whether to show the the path of header code locations with full paths or relative paths. Use `true` to use the full path for header locations. Use `false` or `null` or omit this property to show only relative file paths.")]
    public bool? IsEnabledLocationFullPaths { get; set; }

    [JsonPropertyName("is_enabled_macro_objects")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude macro objects. Use `true` or omit this property to include macro objects. Use `false` to exclude macro objects.")]
    public bool? IsEnabledMacroObjects { get; set; }
}
