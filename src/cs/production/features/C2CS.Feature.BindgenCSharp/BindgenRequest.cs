// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.BindgenCSharp.Data;

namespace C2CS.Feature.BindgenCSharp;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class must have a unique name across namespaces for usage in System.Text.Json source generators.
public class BindgenRequest : UseCaseRequest
{
    /// <summary>
    ///     Path of the input abstract syntax tree directory. The directory should contain one or more previously
    ///     generated abstract syntax tree `.json` files which each have a file name of the target platform.
    /// </summary>
    public string? InputFileDirectory { get; set; }

    /// <summary>
    ///     Path of the output C# `.cs` file.
    /// </summary>
    public string? OutputFilePath { get; set; }

    /// <summary>
    ///     The name of the dynamic link library (without the file extension) used for platform invoke (P/Invoke) with
    ///     C#. If not specified, the library name is the same as the name of the <see cref="OutputFilePath" /> without
    ///     the directory name and without the file extension.
    /// </summary>
    public string? LibraryName { get; set; }

    /// <summary>
    ///     The name of the namespace to be used for the C# static class. If not specified, the namespace is the same as the
    ///     <see cref="LibraryName" />.
    /// </summary>
    public string? NamespaceName { get; set; }

    /// <summary>
    ///     The name of the C# static class. If not specified, the class name is the same as the
    ///     <see cref="LibraryName" />.
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    ///     Path of the text file which to add the file's contents to the top of the C# file. Useful for comments, extra
    ///     namespace using statements, or additional code that needs to be added to the generated C# file.
    /// </summary>
    public string? HeaderCodeRegionFilePath { get; set; }

    /// <summary>
    ///     Path of the text file which to add the file's contents to the bottom of the C# file. Useful for comments or
    ///     additional code that needs to be added to the generated C# file.
    /// </summary>
    public string? FooterCodeRegionFilePath { get; set; }

    /// <summary>
    ///     Pairs of strings for re-mapping type names. Each pair has source name and a target name. Does not change the
    ///     bit layout of types.
    /// </summary>
    public ImmutableArray<CSharpTypeAlias>? MappedTypeNames { get; set; }

    /// <summary>
    ///     Names of types, functions, enums, constants, or anything else that may be found when parsing C code that
    ///     will be ignored when generating C# code. Type names are ignored after mapping type names using
    ///     <see cref="MappedTypeNames" />.
    /// </summary>
    public ImmutableArray<string?>? IgnoredNames { get; set; }
}
