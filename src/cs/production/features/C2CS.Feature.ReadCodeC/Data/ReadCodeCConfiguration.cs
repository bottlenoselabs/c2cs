// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using C2CS.Foundation.UseCases;
using JetBrains.Annotations;

namespace C2CS.Feature.ReadCodeC.Data;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
// NOTE: This class must have a unique name across namespaces for usage in System.Text.Json source generators.
[PublicAPI]
public sealed class ReadCodeCConfiguration : UseCaseConfiguration
{
    [JsonPropertyName("output_file_directory")]
    [Json.Schema.Generation.Description("Path of the output abstract syntax tree directory. The directory will contain one or more generated abstract syntax tree `.json` files which each have a file name of the target platform.")]
    public string? OutputFileDirectory { get; set; }

    [JsonPropertyName("input_file")]
    [Json.Schema.Generation.Description("Path of the input `.h` header file containing C code.")]
    public string? InputFilePath { get; set; }

    [JsonPropertyName("user_include_directories")]
    [Json.Schema.Generation.Description("The directories to search for non-system header files.")]
    public ImmutableArray<string?>? UserIncludeDirectories { get; set; }

    [JsonPropertyName("system_include_directories")]
    [Json.Schema.Generation.Description("The directories to search for system header files.")]
    public ImmutableArray<string?>? SystemIncludeDirectories { get; set; }

    [JsonPropertyName("functions_allowed")]
    [Json.Schema.Generation.Description("The C function names to explicitly include when parsing C code. Default is `null`. If `null`, all functions found may be included. Note that C function names which are excluded may also exclude any transitive types.")]
    public ImmutableArray<string?>? FunctionNamesAllowed { get; set; }

    [JsonPropertyName("opaque_types")]
    [Json.Schema.Generation.Description("Type names that may be found when parsing C code that will be interpreted as opaque types. Opaque types are often used with a pointer to hide the information about the bit layout behind the pointer.")]
    public ImmutableArray<string?>? OpaqueTypeNames { get; set; }

    [JsonPropertyName("is_enabled_location_full_paths")]
    [Json.Schema.Generation.Description("Determines whether to show the the path of header code locations with full paths or relative paths. Use `true` to use the full path for header locations. Use `false` or omit this property to show only relative file paths.")]
    public bool? IsEnabledLocationFullPaths { get; set; }

    [JsonPropertyName("is_enabled_macro_objects")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude macro objects. Use `true` or omit this property to include macro objects. Use `false` to exclude macro objects.")]
    public bool? IsEnabledMacroObjects { get; set; }

    [JsonPropertyName("is_enabled_functions")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude functions. Use `true` or omit this property to include functions. Use `false` to exclude functions.")]
    public bool? IsEnabledFunctions { get; set; }

    [JsonPropertyName("is_enabled_variables")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude variables. Use `true` or omit this property to include variables. Use `false` to exclude variables.")]
    public bool? IsEnabledVariables { get; set; }

    [JsonPropertyName("is_enabled_enums_dangling")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude enums that are transitive to a function or variable. Use `true` to include dangling enums. Use `false` or omit this property to exclude dangling enums.")]
    public bool? IsEnabledEnumsDangling { get; set; }

    [JsonPropertyName("is_enabled_allow_names_with_prefixed_underscore")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude functions, enums, structs, typedefs, etc with a prefixed underscore; such declarations are sometimes considered 'non public'. Use `true` to include declarations with a prefixed underscore. Use `false` or omit this property to exclude declarations with a prefixed underscore.")]
    public bool? IsEnabledAllowNamesWithPrefixedUnderscore { get; set; }

    [JsonPropertyName("is_enabled_system_declarations")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude system declarations. Use `true` to include system functions, enums, typedefs, records, etc. Use `false` or omit this property to exclude system declarations.")]
    public bool? IsEnabledSystemDeclarations { get; set; }

    [JsonPropertyName("is_enabled_find_target_platform_system_headers")]
    [Json.Schema.Generation.Description("Determines whether to automatically find and append the system headers for the target platform. Use `true` or omit this property to automatically find and append system headers for the target platform. Use `false` to not find and append system headers for the target platform.")]
    public bool? IsEnabledFindSystemHeaders { get; set; }

    [JsonPropertyName("platforms")]
    [Json.Schema.Generation.Description("The target platform configurations for extracting the abstract syntax trees. Each target platform is a Clang target triple. See the C2CS docs for more details about what target platforms are available.")]
#pragma warning disable CA2227
    public Dictionary<string, ReadCodeCConfigurationPlatform?>? ConfigurationPlatforms { get; set; }
#pragma warning restore CA2227

    public ImmutableArray<string?>? Frameworks { get; set; }
}
