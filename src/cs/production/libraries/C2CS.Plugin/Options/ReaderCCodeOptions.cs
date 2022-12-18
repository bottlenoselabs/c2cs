// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using JetBrains.Annotations;

namespace C2CS.Options;

// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
[PublicAPI]
public sealed class ReaderCCodeOptions : ExecutorOptions
{
    /// <summary>
    ///     The path of the output abstract syntax tree directory.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The directory will contain one or more generated abstract syntax tree `.json` files which each have a
    ///         file name of the target platform.
    ///     </para>
    /// </remarks>
    public string? OutputAbstractSyntaxTreesFileDirectory { get; set; } = "./ast";

    /// <summary>
    ///     The path of the input `.h` header file containing C code.
    /// </summary>
    public string? InputHeaderFilePath { get; set; }

    /// <summary>
    ///     The directories to search for non-system header files.
    /// </summary>
    public ImmutableArray<string>? UserIncludeDirectories { get; set; }

    /// <summary>
    ///     The directories to search for system header files.
    /// </summary>
    public ImmutableArray<string>? SystemIncludeDirectories { get; set; }

    /// <summary>
    ///     Determines whether to show the the path of header code locations with full paths or relative paths.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>false</c>. Use <c>true</c> to use the full path for header locations. Use <c>false</c> to
    ///         show only relative file paths.
    ///     </para>
    /// </remarks>
    public bool? IsEnabledLocationFullPaths { get; set; }

    /// <summary>
    ///     Determines whether to include or exclude declarations (functions, enums, structs, typedefs, etc) with a
    ///     prefixed underscore that are assumed to be 'non public'.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>false</c>. Use <c>true</c> to include declarations with a prefixed underscore. Use
    ///         <c>false</c> to exclude declarations with a prefixed underscore.
    ///     </para>
    /// </remarks>
    public bool? IsEnabledAllowNamesWithPrefixedUnderscore { get; set; }

    /// <summary>
    ///     Determines whether to include or exclude system declarations (functions, enums, typedefs, records, etc).
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is `false`. Use <c>true</c> to include system declarations. Use `false` to exclude system
    ///         declarations.
    ///     </para>
    /// </remarks>
    public bool? IsEnabledSystemDeclarations { get; set; }

    /// <summary>
    ///     Determines whether to automatically find and append the system headers for the target platform.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to automatically find and append system headers for the target
    ///         platform. Use <c>false</c> to skip.
    ///     </para>
    /// </remarks>
    public bool? IsEnabledFindSystemHeaders { get; set; }

    /// <summary>
    ///     Determines whether to parse the main input header file and all inclusions as if it were a single translation
    ///     unit.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to parse the the main input header file as if it were a single
    ///         translation unit. Use <c>false</c> to parse each translation unit independently.
    ///     </para>
    /// </remarks>
    public bool? IsEnabledSingleHeader { get; set; }

    /// <summary>
    ///     The target platform configurations for extracting the abstract syntax trees.
    /// </summary>
    public ImmutableDictionary<TargetPlatform, ReaderCCodeOptionsPlatform>? Platforms { get; set; }

    /// <summary>
    ///     The names of libraries and/or interfaces for macOS, iOS, tvOS or watchOS.
    /// </summary>
    public ImmutableArray<string>? Frameworks { get; set; }

    /// <summary>
    ///     Type names which can come from blocked header files but are passed through without creating diagnostics.
    /// </summary>
    public ImmutableArray<string>? PassThroughTypeNames { get; set; }
}
