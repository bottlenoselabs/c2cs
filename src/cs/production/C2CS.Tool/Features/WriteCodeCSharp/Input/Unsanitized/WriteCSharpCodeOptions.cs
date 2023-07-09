// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using C2CS.Foundation.Tool;
using JetBrains.Annotations;

namespace C2CS.Features.WriteCodeCSharp.Input.Unsanitized;

// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
[PublicAPI]
public sealed class WriteCSharpCodeOptions : ToolUnsanitizedInput
{
    /// <summary>
    ///     The path of the input cross-platform abstract syntax tree.
    /// </summary>
    [JsonPropertyName("inputFilePath")]
    public string? InputAbstractSyntaxTreeFilePath { get; set; }

    /// <summary>
    ///     The path of the output C# `.cs` file.
    /// </summary>
    [JsonPropertyName("outputFileDirectory")]
    public string? OutputCSharpCodeFileDirectory { get; set; }

    /// <summary>
    ///     The name of the dynamic link library (without the file extension) used for platform invoke (P/Invoke) with
    ///     C#.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>null</c>. If <see cref="LibraryName" /> is <c>null</c>, the file name of
    ///         <see cref="OutputCSharpCodeFileDirectory" /> without the file extension is used.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("libraryName")]
    public string? LibraryName { get; set; }

    /// <summary>
    ///     The name of the namespace to be used for the C# static class.
    /// </summary>
    /// <remarks>
    ///     <para>Default is <c>null</c>. If <see cref="NamespaceName" /> is <c>null</c>, <see cref="LibraryName" /> is used.</para>
    /// </remarks>
    [JsonPropertyName("namespaceName")]
    public string? NamespaceName { get; set; }

    /// <summary>
    ///     The name of the C# static class.
    /// </summary>
    /// <remarks>
    ///     <para>Default is <c>null</c>. If <see cref="ClassName" /> is <c>null</c>, <see cref="LibraryName" /> is used.</para>
    /// </remarks>
    [JsonPropertyName("className")]
    public string? ClassName { get; set; }

    /// <summary>
    ///     The path of the text file which to add the file's contents to the top of the C# file.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="HeaderCodeRegionFilePath" /> is useful for comments, extra namespace using statements, or
    ///         additional code that needs to be added to the top of generated C# file.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("headerCodeRegionFilePath")]
    public string? HeaderCodeRegionFilePath { get; set; }

    /// <summary>
    ///     The path of the text file which to add the file's contents to the bottom of the C# file.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="FooterCodeRegionFilePath" /> is useful for comments or additional code that needs to be added
    ///         to the generated C# file.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("footerCodeRegionFilePath")]
    public string? FooterCodeRegionFilePath { get; set; }

    /// <summary>
    ///     The pairs of strings for re-mapping names where each pair has source name and a target name.
    /// </summary>
    /// <remarks>
    ///     <para><see cref="MappedNames" /> does not change the bit layout of types.</para>
    /// </remarks>
    [JsonPropertyName("mappedNames")]
    public ImmutableArray<WriteCSharpCodeOptionsMappedName>? MappedNames { get; set; }

    /// <summary>
    ///     The pairs of strings for re-mapping C namespaces to C# namespaces.
    /// </summary>
    [JsonPropertyName("mappedCNamespaces")]
    public ImmutableArray<WriteCSharpCodeOptionsMappedName>? MappedCNamespaces { get; set; }

    /// <summary>
    ///     The names of types, functions, enums, constants, or anything else that may be found when parsing C code that
    ///     will be ignored when generating C# code.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Type names are ignored after mapping type names using <see cref="MappedNames" />.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("ignoredNames")]
    public ImmutableArray<string?>? IgnoredNames { get; set; }

    /// <summary>
    ///     Determines whether to use C# 9 function pointers or C# delegates for C function pointers.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to generate C function pointers as C# 9 function pointers. Use
    ///         <c>false</c> to fallback to generate C function pointers as C# delegates.
    ///     </para>
    ///     <para>
    ///         If you have the choice, C# delegates are not recommended in comparison to C# function pointers as they
    ///         require more setup, teardown, and memory allocations.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledFunctionPointers")]
    public bool? IsEnabledFunctionPointers { get; set; } = true;

    /// <summary>
    ///     Determines whether to verify the generated C# code compiles without warnings or errors.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to verify the generated C# code compiles without warnings or
    ///         errors. Use <c>false</c> to disable verifying that the generated C# code compiles.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledVerifyCSharpCodeCompiles")]
    public bool? IsEnabledVerifyCSharpCodeCompiles { get; set; } = true;

    /// <summary>
    ///     Determines whether to enable generating the C# runtime glue code.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to generate C# runtime glue code as part of bindgen. Use
    ///         <c>false</c> to disable generating C# runtime glue code.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledGeneratingRuntimeCode")]
    public bool? IsEnabledGeneratingRuntimeCode { get; set; } = true;

    /// <summary>
    ///     Determines whether to enable C# source code generation using
    ///     <see cref="System.Runtime.InteropServices.LibraryImportAttribute" /> or
    ///     <see cref="System.Runtime.InteropServices.DllImportAttribute" />.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>false</c>. Use <c>true</c> to generate C# source code using
    ///         <see cref="System.Runtime.InteropServices.LibraryImportAttribute" /> . Use <c>false</c> to generate C#
    ///         source code using <see cref="System.Runtime.InteropServices.DllImportAttribute" />.
    ///     </para>
    ///     <para>
    ///         The <see cref="System.Runtime.InteropServices.LibraryImportAttribute" /> is only available in .NET 7.
    ///         The advantages of using <see cref="System.Runtime.InteropServices.LibraryImportAttribute" /> over
    ///         <see cref="System.Runtime.InteropServices.DllImportAttribute" /> is that source generators are used to
    ///         create stubs at compile time instead of runtime. This can increase performance due to IL trimming and
    ///         inlining; make debugging easier with cleaner stack traces; and adds support for full NativeAOT scenarios
    ///         where the <see cref="System.Runtime.InteropServices.DllImportAttribute" /> is not available.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledLibraryImport")]
    public bool? IsEnabledLibraryImport { get; set; } = false;

    /// <summary>
    ///     Determines whether to enable generating the C# assembly attribute usages at the scope of the main namespace.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to enabled generation of assembly attributes usages which are
    ///         applied at the scope of the main namespace. Use <c>false</c> to disable generation of assembly attribute
    ///         usages.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledGenerateAssemblyAttributes")]
    public bool? IsEnabledGenerateAssemblyAttributes { get; set; } = true;

    /// <summary>
    ///     Determines whether to enable generating idiomatic C#.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>false</c>. Use <c>true</c> to enabled generation of idiomatic C# which includes converting
    ///         converting C `snake_case` names to C# `PascalCase` names. Use <c>false</c> to leave names as they are
    ///         found in C.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledIdiomaticCSharp")]
    public bool? IsEnabledIdiomaticCSharp { get; set; } = false;

    /// <summary>
    ///     Determines whether to parse enum value names as upper-case when generating idiomatic C#.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to enable parsing of enum value names as upper-case during
    ///         generation of idiomatic C#. Use <c>false</c> to leave enum value names as they are found in C.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledEnumValueNamesUpperCase")]
    public bool? IsEnabledEnumValueNamesUpperCase { get; set; } = true;
}
