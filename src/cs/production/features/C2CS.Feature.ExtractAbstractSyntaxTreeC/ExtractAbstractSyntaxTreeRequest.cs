// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC;

// NOTE: Properties are required for System.Text.Json serialization
// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
// NOTE: This class must have a unique name across namespaces for usage in System.Text.Json source generators.
public class ExtractAbstractSyntaxTreeRequest : UseCaseRequest
{
    /// <summary>
    ///     Path of the input `.h` header file.
    /// </summary>
    public string? InputFilePath { get; set; }

    /// <summary>
    ///     Path of the output abstract syntax tree directory. The directory will be filled with a `.json` file which
    ///     the file name will be <see cref="TargetPlatform" />.
    /// </summary>
    public string? OutputFileDirectory { get; set; }

    /// <summary>
    ///     Determines whether the software development kit (SDK) for C/C++ is attempted to be found. Default is
    ///     <c>true</c>. If <c>true</c>, the C/C++ header files for the current operating system are attempted to be
    ///     found by some reasonable means. If the C/C++ header files can not be found, then an error is generated which
    ///     halts the program. If <c>false</c>, the C/C++ header files will likely be missing causing Clang to generate
    ///     parsing errors which also halts the program. In such a case, the missing C/C++ header files can be supplied
    ///     to Clang using <see cref="ClangArguments" /> such as <c>"-isystemPATH/TO/SYSTEM/HEADER/DIRECTORY"</c>.
    /// </summary>
    public bool? IsEnabledFindSdk { get; set; } = true;

    /// <summary>
    ///     The target platform to use when parsing C code. Default is <c>null</c>. If <c>null</c>, the host platform is
    ///     used. If not <c>null</c> nand the value of the host platform, the C code is parsed using cross compilation.
    ///     Possible values are <c>null</c>,
    ///     <c>win-x64</c>, <c>osx-x64</c>, <c>osx-arm64</c>, <c>linux-x64</c>.
    /// </summary>
    public string? TargetPlatform { get; set; }

    /// <summary>
    ///     Search directory paths to use for `#include` usages when parsing C code.
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
    ///     The C function names to explicitly include when parsing C code. Default is <c>null</c>. If <c>null</c>,
    ///     no white list applies. Note that C function names which are excluded also exclude any transitive types.
    /// </summary>
    public ImmutableArray<string?>? FunctionNamesWhiteList { get; set; }

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
