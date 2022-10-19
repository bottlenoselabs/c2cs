// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using JetBrains.Annotations;

namespace C2CS.Options;

// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
[PublicAPI]
public sealed class WriterOptionsCSharpCode : UseCaseOptions
{
    /// <summary>
    ///     The path of the input abstract syntax tree directory. The directory should contain one or more previously
    ///     generated abstract syntax tree `.json` files which each have a file name of the target platform.
    /// </summary>
    public string? InputFileDirectory { get; set; }

    /// <summary>
    ///     The path of the output C# `.cs` file.
    /// </summary>
    public string? OutputFilePath { get; set; }

    /// <summary>
    ///     The name of the dynamic link library (without the file extension) used for platform invoke (P/Invoke) with
    ///     C#.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>null</c>. If <see cref="LibraryName" /> is <c>null</c>, the file name of <see cref="OutputFilePath" /> without
    ///         the file extension is used.
    ///     </para>
    /// </remarks>
    public string? LibraryName { get; set; }

    /// <summary>
    ///     The name of the namespace to be used for the C# static class.
    /// </summary>
    /// <remarks>
    ///     <para>Default is <c>null</c>. If <see cref="NamespaceName" /> is <c>null</c>, <see cref="LibraryName" /> is used.</para>
    /// </remarks>
    public string? NamespaceName { get; set; }

    /// <summary>
    ///     The name of the C# static class.
    /// </summary>
    /// <remarks>
    ///     <para>Default is <c>null</c>. If <see cref="ClassName" /> is <c>null</c>, <see cref="LibraryName" /> is used.</para>
    /// </remarks>
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
    public string? FooterCodeRegionFilePath { get; set; }

    /// <summary>
    ///     The pairs of strings for re-mapping names where each pair has source name and a target name.
    /// </summary>
    /// <remarks>
    ///     <para><see cref="MappedNames" /> does not change the bit layout of types.</para>
    /// </remarks>
    public ImmutableArray<WriterOptionsCSharpCodeMappedName>? MappedNames { get; set; }

    /// <summary>
    ///     The names of types, functions, enums, constants, or anything else that may be found when parsing C code that
    ///     will be ignored when generating C# code.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Type names are ignored after mapping type names using <see cref="MappedNames" />.
    ///     </para>
    /// </remarks>
    public ImmutableArray<string?>? IgnoredNames { get; set; }

    /// <summary>
    ///     Determines whether to pre-compile (pre-JIT) the C# API on setup (first load).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to pre-compile the C# API code. Use <c>false</c> to disable
    ///         pre-compile of the C# API code. Note that if using the C# bindings in the context of NativeAOT this
    ///         should be disabled.
    ///     </para>
    /// </remarks>
    public bool? IsEnabledPreCompile { get; set; } = true;

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
    public bool? IsEnabledFunctionPointers { get; set; } = true;
}
