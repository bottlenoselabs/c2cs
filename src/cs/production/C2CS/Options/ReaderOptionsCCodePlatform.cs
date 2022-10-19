// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using JetBrains.Annotations;

namespace C2CS.Options;

// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
[PublicAPI]
public sealed class ReaderOptionsCCodePlatform
{
    /// <summary>
    ///     The directories to search for non-system header files specific to the target platform.
    /// </summary>
    public ImmutableArray<string>? UserIncludeFileDirectories { get; set; }

    /// <summary>
    ///     The directories to search for system header files of the target platform.
    /// </summary>
    public ImmutableArray<string>? SystemIncludeFileDirectories { get; set; }

    /// <summary>
    ///     The object-like macros to use when parsing C code.
    /// </summary>
    public ImmutableArray<string>? Defines { get; set; }

    /// <summary>
    ///     C header file paths to exclude from generating root nodes such as functions or variables.
    /// </summary>
    /// <remarks>
    ///     <para>File paths are relative to <see cref="UserIncludeFileDirectories" />.</para>
    ///     <para>
    ///         Because the way libclang works this does not block transitive files. In other words it only blocks files
    ///         where the declarations ares defined.
    ///     </para>
    /// </remarks>
    public ImmutableArray<string>? HeaderFilesBlocked { get; set; }

    /// <summary>
    ///     The additional Clang arguments to use when parsing C code.
    /// </summary>
    public ImmutableArray<string>? ClangArguments { get; set; }

    /// <summary>
    ///     The names of libraries and/or interfaces for macOS, iOS, tvOS or watchOS.
    /// </summary>
    public ImmutableArray<string>? Frameworks { get; set; }
}
