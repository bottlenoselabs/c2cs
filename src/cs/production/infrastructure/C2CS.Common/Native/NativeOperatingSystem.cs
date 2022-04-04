// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace C2CS;

/// <summary>
///     Defines the native operating systems.
/// </summary>
[PublicAPI]
public enum NativeOperatingSystem
{
    /// <summary>
    ///     Unknown operating system.
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     Versions of Windows operating system.
    /// </summary>
    Windows = 1,

    /// <summary>
    ///     Versions of macOS operating system.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
    [SuppressMessage(
        "StyleCop.Naming",
        "SA1300:ElementMustBeginWithUpperCaseLetter",
        Justification = "Product name.")]
    macOS = 2,

    /// <summary>
    ///     Distributions of the Linux operating system.
    /// </summary>
    Linux = 3,

    /// <summary>
    ///     Versions of FreeBSD operating system.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
    [SuppressMessage(
        "StyleCop.Naming",
        "SA1300:ElementMustBeginWithUpperCaseLetter",
        Justification = "Product name.")]
    FreeBSD = 4,

    /// <summary>
    ///     Mobile versions of Android operating system.
    /// </summary>
    Android = 5,

    /// <summary>
    ///     Mobile versions of iOS operating system.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
    [SuppressMessage(
        "StyleCop.Naming",
        "SA1300:ElementMustBeginWithUpperCaseLetter",
        Justification = "Product name.")]
    iOS = 6,

    /// <summary>
    ///     Micro console versions of tvOS operating system.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
    [SuppressMessage(
        "StyleCop.Naming",
        "SA1300:ElementMustBeginWithUpperCaseLetter",
        Justification = "Product name.")]
    tvOS = 7,

    /// <summary>
    ///     Not really an operating system, but rather versions of WebAssembly on some WASI
    ///     (WebAssembly System Interface) compliant host program such as a modern web browser.
    /// </summary>
    Browser = 8,

    /// <summary>
    ///     Versions of the PlayStation operating system. Otherwise known as "Orbis OS". Based on <see cref="FreeBSD" />.
    /// </summary>
    PlayStation4 = 9,

    /// <summary>
    ///     Versions of the Xbox operating system.
    /// </summary>
    Xbox = 10,

    /// <summary>
    ///     Versions of the Nintendo Switch operating system.
    /// </summary>
    Switch = 11,

    /// <summary>
    ///     Versions of the Nintendo 3DS operating system.
    /// </summary>
    DualScreen3D = 12
}
