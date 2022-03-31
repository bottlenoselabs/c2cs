// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Feature.WriteCodeCSharp.Data;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class must have a unique name across namespaces for usage in System.Text.Json source generators.
[PublicAPI]
public sealed class WriteCodeCSharpConfiguration : UseCaseConfiguration
{
    [JsonIgnore]
    [Json.Schema.Generation.Description("Path of the input abstract syntax tree directory. The directory should contain one or more previously generated abstract syntax tree `.json` files which each have a file name of the target platform.")]
    public string? InputFileDirectory { get; set; }

    [JsonPropertyName("output_file")]
    [Json.Schema.Generation.Description("Path of the output C# `.cs` file.")]
    public string? OutputFilePath { get; set; }

    [JsonPropertyName("library_name")]
    [Json.Schema.Generation.Description("The name of the dynamic link library (without the file extension) used for platform invoke (P/Invoke) with C#. If not specified, the library name is the same as the name of the `OutputFilePath` property without the directory name and without the file extension.")]
    public string? LibraryName { get; set; }

    [JsonPropertyName("namespace_name")]
    [Json.Schema.Generation.Description("The name of the namespace to be used for the C# static class. If not specified, the namespace is the same as the `LibraryName` property.")]
    public string? NamespaceName { get; set; }

    [JsonPropertyName("class_name")]
    [Json.Schema.Generation.Description("The name of the C# static class. If not specified, the class name is the same as the `LibraryName` property.")]
    public string? ClassName { get; set; }

    [JsonPropertyName("region_header_file")]
    [Json.Schema.Generation.Description("Path of the text file which to add the file's contents to the top of the C# file. Useful for comments, extra namespace using statements, or additional code that needs to be added to the generated C# file.")]
    public string? HeaderCodeRegionFilePath { get; set; }

    [JsonPropertyName("region_footer_file")]
    [Json.Schema.Generation.Description("Path of the text file which to add the file's contents to the bottom of the C# file. Useful for comments or additional code that needs to be added to the generated C# file.")]
    public string? FooterCodeRegionFilePath { get; set; }

    [JsonPropertyName("mapped")]
    [Json.Schema.Generation.Description("Pairs of strings for re-mapping type names. Each pair has source name and a target name. Does not change the bit layout of types.")]
    public ImmutableArray<CSharpTypeAlias>? MappedTypeNames { get; set; }

    [JsonPropertyName("ignored")]
    [Json.Schema.Generation.Description("Names of types, functions, enums, constants, or anything else that may be found when parsing C code that will be ignored when generating C# code. Type names are ignored after mapping type names using `MappedTypeNames` property.")]
    public ImmutableArray<string?>? IgnoredNames { get; set; }
}
