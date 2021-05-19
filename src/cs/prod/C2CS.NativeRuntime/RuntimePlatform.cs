// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;

/// <summary>
///     Defines the native runtime platforms (operating system + computer architecture).
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBeInternal", Justification = "Public API.")]
public enum RuntimePlatform
{
    /// <summary>
    ///     Unknown target platform.
    /// </summary>
    Unknown = 0,

    /// <summary>
    ///     Desktop versions of Windows on either 32-bit or 64-bit computing architecture.
    /// </summary>
    Windows = 1,

    /// <summary>
    ///     Desktop versions of macOS on 64-bit computing architecture.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
    [SuppressMessage("StyleCop.Naming", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Product name.")]
    macOS = 2,

    /// <summary>
    ///     Desktop distributions of the Linux operating system on 64-bit computing architecture.
    /// </summary>
    Linux = 3,

    /// <summary>
    ///     Mobile versions of Android on 64-bit computing architecture.
    /// </summary>
    Android = 4,

    /// <summary>
    ///     Mobile versions of iOS (Apple) on 64-bit computing architecture.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
    [SuppressMessage("StyleCop.Naming", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Product name.")]
    iOS = 5,

    /// <summary>
    ///     Versions of WebAssembly (64-bit) on some WASI (WebAssembly System Interface) compliant host program such as a modern web browser.
    /// </summary>
    Web,

    // TODO: tvOS, RaspberryPi, WebAssembly, PlayStation4, PlayStationVita, Switch etc
}
