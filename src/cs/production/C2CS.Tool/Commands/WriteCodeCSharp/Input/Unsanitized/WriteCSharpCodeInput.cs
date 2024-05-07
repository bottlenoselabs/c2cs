// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using bottlenoselabs.Common.Tools;
using JetBrains.Annotations;

namespace C2CS.Commands.WriteCodeCSharp.Input.Unsanitized;

// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
[PublicAPI]
public sealed class WriteCSharpCodeInput : ToolUnsanitizedInput
{
    /// <summary>
    ///     Gets or sets the path of the input cross-platform FFI json file.
    /// </summary>
    [JsonPropertyName("inputFilePath")]
    public string? InputCrossPlatformFfiFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the path of the output C# `.cs` file.
    /// </summary>
    [JsonPropertyName("outputFileDirectory")]
    public string? OutputCSharpCodeFileDirectory { get; set; }

    /// <summary>
    ///     Gets or sets the name of the dynamic link library (without the file extension) used for platform invoke
    ///     (P/Invoke) with C#.
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
    ///     Gets or sets the name of the namespace to be used for the C# static class.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>null</c>. If <see cref="NamespaceName" /> is <c>null</c>, no namespace is explicitly used
    ///         and thus the default implicit global namespace 'global::' is used.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("namespaceName")]
    public string? NamespaceName { get; set; }

    /// <summary>
    ///     Gets or sets the name of the C# static class.
    /// </summary>
    /// <remarks>
    ///     <para>Default is <c>null</c>. If <see cref="ClassName" /> is <c>null</c>, <see cref="LibraryName" /> is used.</para>
    /// </remarks>
    [JsonPropertyName("className")]
    public string? ClassName { get; set; }

    /// <summary>
    ///     Gets or sets the path of the text file which to add the file's contents to the top of the C# file.
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
    ///     Gets or sets the path of the text file which to add the file's contents to the bottom of the C# file.
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
    ///     Gets or sets the pairs of strings for re-mapping names where each pair has source name and a target name.
    /// </summary>
    /// <remarks>
    ///     <para><see cref="MappedNames" /> does not change the bit layout of types.</para>
    /// </remarks>
    [JsonPropertyName("mappedNames")]
    public ImmutableArray<WriteCSharpCodeInputMappedName>? MappedNames { get; set; }

    /// <summary>
    ///     Gets or sets the pairs of strings for re-mapping C namespaces to C# namespaces.
    /// </summary>
    [JsonPropertyName("mappedCNamespaces")]
    public ImmutableArray<WriteCSharpCodeInputMappedName>? MappedCNamespaces { get; set; }

    /// <summary>
    ///     Gets or sets the names of types, functions, enums, constants, or anything else that may be found when
    ///     parsing C code that will be ignored when generating C# code.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Type names are ignored after mapping type names using <see cref="MappedNames" />.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("ignoredNames")]
    public ImmutableArray<string?>? IgnoredNames { get; set; }

    /// <summary>
    ///     Gets or sets the Target Framework Moniker (TFM) used for generating C# code.
    /// </summary>
    /// <remarks>
    ///     <para>See https://learn.microsoft.com/en-us/dotnet/standard/frameworks#latest-versions for list of valid TFMs.</para>
    /// </remarks>
    [JsonPropertyName("targetFrameworkMoniker")]
    public string? TargetFrameworkMoniker { get; set; }

    /// <summary>
    ///     Gets or sets whether to verify the generated C# code compiles without warnings or errors is enabled.
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
    ///     Gets or sets whether enable generating the C# runtime glue code is enabled.
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
    ///     Gets or sets whether generating idiomatic C# is enabled.
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
    ///     Gets or sets whether parsing enum value names as upper-case when generating idiomatic C# is enabled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to enable parsing of enum value names as upper-case during
    ///         generation of idiomatic C#. Use <c>false</c> to leave enum value names as they are found in C.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledEnumValueNamesUpperCase")]
    public bool? IsEnabledEnumValueNamesUpperCase { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether function pointers are enabled for unmanaged callbacks.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c> when <see cref="TargetFrameworkMoniker" /> is at least .NET Core 5 or greater
    ///         and <c>false</c> otherwise. Use <c>true</c> to generate C# function pointers for unmanaged callbacks.
    ///         Use <c>false</c> to fallback to using C# delegates for callbacks.
    ///     </para>
    ///     <para>
    ///         If you targeting .NET Core 5 or greater, you have the choice to use function pointers or delegates.
    ///         However, C# delegates are not recommended in comparison to C# function pointers as they require more
    ///         setup, teardown, memory allocations, and a level of indirection.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledFunctionPointers")]
    public bool? IsEnabledFunctionPointers { get; set; } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether runtime marshalling is enabled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>false</c> when <see cref="TargetFrameworkMoniker" /> is at least .NET Core 7 or greater
    ///         and <c>true</c> otherwise. Use <c>true</c> to enable runtime marshalling. Use <c>false</c> to disable
    ///         runtime marshalling.
    ///     </para>
    ///     <para>
    ///         Disabling runtime marshalling is preferred for performance as it removes any possible slow downs that
    ///         could happen at runtime in transforming the data types to/from the unmanaged context. For more
    ///         information, see https://learn.microsoft.com/en-us/dotnet/standard/native-interop/disabled-marshalling.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledRuntimeMarshalling")]
    public bool? IsEnabledRuntimeMarshalling { get; set; }

    /// <summary>
    ///     Gets or sets whether creating a C# namespace scope is enabled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c> when <see cref="TargetFrameworkMoniker" /> is at least .NET Core 6 or greater
    ///         and <c>true</c> otherwise. Use <c>true</c> to enable file scoped namespace when generating C# code. Use
    ///         <c>false</c> to disable file scoped namespace and fallback to traditional namespace scope.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledFileScopedNamespace")]
    public bool? IsEnabledFileScopedNamespace { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether C# source code generation using
    ///     <see cref="System.Runtime.InteropServices.LibraryImportAttribute" /> is enabled or
    ///     <see cref="System.Runtime.InteropServices.DllImportAttribute" /> is enabled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c> when <see cref="TargetFrameworkMoniker" /> is at least .NET Core 7 or greater
    ///         and <c>false</c> otherwise. Use <c>true</c> to generate C# source code using
    ///         <see cref="System.Runtime.InteropServices.LibraryImportAttribute" />. Use <c>false</c> to generate C#
    ///         source code using <see cref="System.Runtime.InteropServices.DllImportAttribute" />.
    ///     </para>
    ///     <para>
    ///         For more information see:
    ///         https://learn.microsoft.com/en-us/dotnet/standard/native-interop/pinvoke-source-generation.
    ///     </para>
    /// </remarks>
    [JsonPropertyName("isEnabledLibraryImport")]
    public bool? IsEnabledLibraryImport { get; set; } = true;
}
