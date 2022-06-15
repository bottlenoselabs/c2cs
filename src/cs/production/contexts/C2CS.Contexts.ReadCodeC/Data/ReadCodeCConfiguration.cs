// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using C2CS.Foundation.Data;
using JetBrains.Annotations;

namespace C2CS;

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

    [JsonPropertyName("is_enabled_functions")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude functions. Use `true` or to include functions. Use `false` to exclude functions. Default is `true`. See the `functions` property to control which ones are explicitly allowed.")]
    public bool? IsEnabledFunctions { get; set; }

    [JsonPropertyName("functions_allowed")]
    [Json.Schema.Generation.Description("The function names to explicitly include. Default is `null`. If `null`, all functions found may be included only if `is_enabled_functions` is `true`. Note that function which are excluded may also exclude any transitive types.")]
    public ImmutableArray<string?>? FunctionNamesAllowed { get; set; }

    [JsonPropertyName("functions_blocked")]
    [Json.Schema.Generation.Description("The function names to explicitly exclude. Default is `null`. Note that function which are excluded may also exclude any transitive types.")]
    public ImmutableArray<string?>? FunctionNamesBlocked { get; set; }

    [JsonPropertyName("is_enabled_variables")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude variables. Use `true` to include variables. Use `false` to exclude variables. Default is `true`. See the `variables` property to control which ones are explicitly allowed.")]
    public bool? IsEnabledVariables { get; set; }

    [JsonPropertyName("variables_allowed")]
    [Json.Schema.Generation.Description("The variable names to explicitly include. Default is `null`. If `null`, all variables found may be included only if `is_enabled_variables` is `true`. Note that variables which are excluded may also exclude any transitive types.")]
    public ImmutableArray<string?>? VariableNamesAllowed { get; set; }

    [JsonPropertyName("is_enabled_macro_objects")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude macro objects. Use `true` to include macro objects. Use `false` to exclude all macro objects. Default is `true`. See the `macro_objects` property to control which ones are explicitly allowed.")]
    public bool? IsEnabledMacroObjects { get; set; }

    [JsonPropertyName("macro_objects_allowed")]
    [Json.Schema.Generation.Description("The macro object names to explicitly include. Default is `null`. If `null`, all macro objects found may be included only if `is_enabled_macro_objects` is `true`. Note that macro objects which are excluded may also exclude any transitive types.")]
    public ImmutableArray<string?>? MacroObjectNamesAllowed { get; set; }

    [JsonPropertyName("is_enabled_enum_constants")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude enum constants. Use `true` to include enum constants. Use `false` to exclude all enum constants. Default is `false`. See the `enum_constants` property to control which ones are explicitly allowed.")]
    public bool? IsEnabledEnumConstants { get; set; }

    [JsonPropertyName("enum_constants_allowed")]
    [Json.Schema.Generation.Description("The enum constant names to explicitly include. Default is `null`. If `null`, all enum constants found may be included only if `is_enabled_enum_constants` is `true`.")]
    public ImmutableArray<string?>? EnumConstantNamesAllowed { get; set; }

    [JsonPropertyName("is_enabled_dangling_enums")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude enums that are independent to a root node such as a function or variable. Use `true` to include dangling enums. Use `false` to exclude all dangling enums. Default is `false`. See the `dangling_enums` property to control which ones are explicitly allowed.")]
    public bool? IsEnabledEnumsDangling { get; set; }

    [JsonPropertyName("dangling_enums_allowed")]
    [Json.Schema.Generation.Description("The dangling enum names to explicitly include. Default is `null`. If `null`, all dangling enums found may be included only if `is_enabled_dangling_enums` is `true`.")]
    public ImmutableArray<string?>? DanglingEnumNamesAllowed { get; set; }

    [JsonPropertyName("opaque_types")]
    [Json.Schema.Generation.Description("Type names that may be found when parsing C code that will be re-interpreted as opaque types. Opaque types are often used with a pointer to hide the information about the bit layout behind the pointer.")]
    public ImmutableArray<string?>? OpaqueTypeNames { get; set; }

    [JsonPropertyName("is_enabled_location_full_paths")]
    [Json.Schema.Generation.Description("Determines whether to show the the path of header code locations with full paths or relative paths. Use `true` to use the full path for header locations. Use `false` or omit this property to show only relative file paths.")]
    public bool? IsEnabledLocationFullPaths { get; set; }

    [JsonPropertyName("is_enabled_allow_names_with_prefixed_underscore")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude functions, enums, structs, typedefs, etc with a prefixed underscore; such declarations are sometimes considered 'non public'. Use `true` to include declarations with a prefixed underscore. Use `false` or omit this property to exclude declarations with a prefixed underscore.")]
    public bool? IsEnabledAllowNamesWithPrefixedUnderscore { get; set; }

    [JsonPropertyName("is_enabled_system_declarations")]
    [Json.Schema.Generation.Description("Determines whether to include or exclude system declarations. Use `true` to include system functions, enums, typedefs, records, etc. Use `false` to exclude system declarations. Default is `false`.")]
    public bool? IsEnabledSystemDeclarations { get; set; }

    [JsonPropertyName("is_enabled_find_system_headers")]
    [Json.Schema.Generation.Description("Determines whether to automatically find and append the system headers for the target platform. Use `true` to automatically find and append system headers for the target platform. Use `false` to not find and append system headers for the target platform. Default is `true`.")]
    public bool? IsEnabledFindSystemHeaders { get; set; }

    [JsonPropertyName("header_files_blocked")]
    [Json.Schema.Generation.Description("C header file paths to exclude from generating root nodes such as functions or variables. File paths are relative to the `IncludeDirectories` property.")]
    public ImmutableArray<string?>? HeaderFilesBlocked { get; set; }

    [Json.Schema.Generation.Description("Determines whether to parse the main input header file and all inclusions as if it were a single translation unit. Use `true` to parse the the main input header file as if it were a single translation unit. Use `false` to parse each translation unit independently. Default is `true`.")]
    [JsonPropertyName("is_enabled_single_header")]
    public bool? IsEnabledSingleHeader { get; set; }

    [JsonPropertyName("platforms")]
    [Json.Schema.Generation.Description("The target platform configurations for extracting the abstract syntax trees. Each target platform is a Clang target triple. See the C2CS docs for more details about what target platforms are available.")]
#pragma warning disable CA2227
    public Dictionary<string, ReadCodeCConfigurationPlatform?>? ConfigurationPlatforms { get; set; }
#pragma warning restore CA2227

    [Json.Schema.Generation.Description("Names of libraries and/or interfaces for macOS, iOS, tvOS or watchOS.")]
    [JsonPropertyName("frameworks")]
    public ImmutableArray<string?>? Frameworks { get; set; }

    [Json.Schema.Generation.Description("Type names which can come from blocked header files but are passed through without creating diagnostics.")]
    [JsonPropertyName("pass_through_types")]
    public ImmutableArray<string?>? PassThroughTypeNames { get; set; }
}
