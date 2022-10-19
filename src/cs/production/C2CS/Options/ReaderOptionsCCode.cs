// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using JetBrains.Annotations;

namespace C2CS.Options;

// NOTE: This class is considered un-sanitized input; all strings and other types could be null.
[PublicAPI]
public sealed class ReaderOptionsCCode : UseCaseOptions
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
    public string? OutputFileDirectory { get; set; }

    /// <summary>
    ///     The path of the input `.h` header file containing C code.
    /// </summary>
    public string? InputFilePath { get; set; }

    /// <summary>
    ///     The directories to search for non-system header files.
    /// </summary>
    public ImmutableArray<string>? UserIncludeDirectories { get; set; }

    /// <summary>
    ///     The directories to search for system header files.
    /// </summary>
    public ImmutableArray<string>? SystemIncludeDirectories { get; set; }

    /// <summary>
    ///     Determines whether to include or exclude macro objects.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>true</c>. Use <c>true</c> to include macro objects. Use <c>false</c> to exclude all macro
    ///         objects. Use the <see cref="MacroObjectNamesAllowed" /> to control which ones are explicitly allowed.
    ///     </para>
    /// </remarks>
    public bool? IsEnabledMacroObjects { get; set; }

    /// <summary>
    ///     The macro object names to explicitly include.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>null</c>. If <c>null</c>, all macro objects found may be included only if
    ///         <see cref="IsEnabledMacroObjects" /> is <c>true</c>. Note that macro objects which are excluded may also
    ///         exclude any transitive types.
    ///     </para>
    /// </remarks>
    public ImmutableArray<string>? MacroObjectNamesAllowed { get; set; }

    /// <summary>
    ///     Determines whether to include or exclude enum constants.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use <c>true</c> to include enum constants. Use <c>false</c> to exclude all enum constants. Default is
    ///         <c>false</c>. Use <see cref="EnumConstantNamesAllowed" /> to control which ones are explicitly allowed.
    ///     </para>
    /// </remarks>
    public bool? IsEnabledEnumConstants { get; set; }

    /// <summary>
    ///     The enum constant names to explicitly include.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>null</c>. If <c>null</c>, all enum constants found may be included only if
    ///         <see cref="IsEnabledEnumConstants" /> is <c>true</c>.
    ///     </para>
    /// </remarks>
    public ImmutableArray<string>? EnumConstantNamesAllowed { get; set; }

    /// <summary>
    ///     Determines whether to include or exclude enums that are independent to a root node such as a function or
    ///     variable.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>false</c>. Use <c>true</c> to include dangling enums. Use <c>false</c> to exclude all
    ///         dangling enums. Use <see cref="EnumDanglingNamesAllowed" /> to control which ones are explicitly allowed.
    ///     </para>
    /// </remarks>
    public bool? IsEnabledEnumsDangling { get; set; }

    /// <summary>
    ///     The dangling enum names to explicitly include.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default is <c>null</c>. If <c>null</c>, all dangling enums found may be included only if
    ///         <see cref="IsEnabledEnumsDangling" /> is <c>true</c>.
    ///     </para>
    /// </remarks>
    public ImmutableArray<string>? EnumDanglingNamesAllowed { get; set; }

    /// <summary>
    ///     Type names that may be found when parsing C code that will be re-interpreted as opaque types.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Opaque types are often used with a pointer to hide the information about the bit layout behind the
    ///         pointer.
    ///     </para>
    /// </remarks>
    public ImmutableArray<string>? OpaqueTypeNames { get; set; }

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
    ///     C header file paths to exclude from generating root nodes such as functions or variables. File paths are
    ///     relative to <see cref="UserIncludeDirectories" />.
    /// </summary>
    public ImmutableArray<string>? HeaderFilesBlocked { get; set; }

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
    public ImmutableDictionary<TargetPlatform, ReaderOptionsCCodePlatform>? Platforms { get; set; }

    /// <summary>
    ///     The names of libraries and/or interfaces for macOS, iOS, tvOS or watchOS.
    /// </summary>
    public ImmutableArray<string>? Frameworks { get; set; }

    /// <summary>
    ///     Type names which can come from blocked header files but are passed through without creating diagnostics.
    /// </summary>
    public ImmutableArray<string>? PassThroughTypeNames { get; set; }
}
