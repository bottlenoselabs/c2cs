// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
public class ProgramConfiguration
{
    /// <summary>
    ///     Path of the input `.h` header file.
    /// </summary>
    public string? InputFilePath { get; set; }

    /// <summary>
    ///     Path of the output C# `.cs` file. If not specified, defaults to a file path using the current directory, a
    ///     file name without extension that matches the <see cref="InputFilePath" />, and a `.cs` file name extension.
    /// </summary>
    public string? OutputFilePath { get; set; }

    /// <summary>
    ///     Path of the intermediate output abstract syntax tree `.json` file. If not specified, defaults to random
    ///     temporary file.
    /// </summary>
    public string? AbstractSyntaxTreeOutputFilePath { get; set; }

    /// <summary>
    ///     The name of the dynamic link library (without the file extension) used for platform invoke (P/Invoke) with
    ///     C#. If not specified, the library name is the same as the name of the <see cref="InputFilePath" /> without
    ///     the directory name and without the file extension.
    /// </summary>
    public string? LibraryName { get; set; }

    /// <summary>
    ///     The name of the namespace to be used for C# static class. If not specified, the namespace is the same as the
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
    ///     Pairs of strings for re-mapping type names. Each pair has source name and a target name. The source name may
    ///     be found when parsing C code and get mapped to the target name when generating C# code. Does not change the
    ///     type's bit layout.
    /// </summary>
    public ImmutableArray<(string? SourceName, string? TargetName)>? MappedTypeNames { get; set; }

    /// <summary>
    ///     Determines whether the software development kit (SDK) for C/C++ is attempted to be found. Default is
    ///     <c>true</c>. If <c>true</c>, the C/C++ header files for the current operating system are attempted to be
    ///     found. In such a case, if the C/C++ header files can not be found, then an error is generated which halts
    ///     the program. If <c>false</c>, the C/C++ header files will likely be missing causing Clang to generate
    ///     parsing errors which also halts the program. In such a case, the missing C/C++ header files can be supplied
    ///     to Clang using <see cref="ClangArguments" /> such as <c>"-isystemPATH/TO/SYSTEM/HEADER/DIRECTORY"</c>.
    /// </summary>
    public bool? IsEnabledFindSdk { get; set; } = true;

    /// <summary>
    ///     The bit width of the computer architecture to use when parsing C code. Default is <c>null</c>. If
    ///     <c>null</c>, the bit width of host operating system's computer architecture is used. E.g. the default for
    ///     x64 Windows is `64`. Possible values are <c>null</c>, <c>32</c> where pointers are 4 bytes, or <c>64</c>
    ///     where pointers are 8 bytes.
    /// </summary>
    public int? MachineBitWidth { get; set; }

    /// <summary>
    ///     Search directory paths to use for `#include` usages when parsing C code.
    ///     If <see cref="ImmutableArray{T}.Empty" />, uses the directory path of <see cref="InputFilePath" />.
    /// </summary>
    public ImmutableArray<string?>? IncludeDirectories { get; set; }

    /// <summary>
    ///     Object-like macros to use when parsing C code.
    /// </summary>
    public ImmutableArray<string?>? Defines { get; set; }

    /// <summary>
    ///     C header file names to exclude. File names are relative to <see cref="IncludeDirectories "/>.
    /// </summary>
    public ImmutableArray<string?>? ExcludedHeaderFiles { get; set; }

    /// <summary>
    ///     Type names that may be found when parsing C code that will be ignored when generating C# code.
    ///     Types are ignored after mapping type names using <see cref="MappedTypeNames" />.
    /// </summary>
    public ImmutableArray<string?>? IgnoredTypeNames { get; set; }

    /// <summary>
    ///     Type names that may be found when parsing C code that will be interpreted as opaque types. Opaque types are
    ///     often used with a pointer to hide the information about the bit layout behind the pointer.
    /// </summary>
    public ImmutableArray<string?>? OpaqueTypeNames { get; set; }

    /// <summary>
    ///     Additional Clang arguments to use when parsing C code.
    /// </summary>
    public ImmutableArray<string?>? ClangArguments { get; set; }
}
