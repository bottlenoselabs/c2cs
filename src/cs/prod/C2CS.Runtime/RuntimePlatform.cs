// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace C2CS
{
    /// <summary>
    ///     Defines the native runtime platforms (operating system + computer architecture).
    /// </summary>
    [PublicAPI]
    public enum RuntimePlatform
    {
        /// <summary>
        ///     Unknown target platform.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     Desktop versions of Windows operating system on either 32-bit or 64-bit computing architecture.
        /// </summary>
        Windows = 1,

        /// <summary>
        ///     Desktop versions of macOS operating system on 64-bit computing architecture.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
        [SuppressMessage("StyleCop.Naming", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Product name.")]
        macOS = 2,

        /// <summary>
        ///     Desktop distributions of the Linux operating system on 64-bit computing architecture.
        /// </summary>
        Linux = 3,

        /// <summary>
        ///     Desktop versions of FreeBSD operating system on 64-bit computing architecture.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
        [SuppressMessage("StyleCop.Naming", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Product name.")]
        FreeBSD = 4,

        /// <summary>
        ///     Mobile versions of Android on 64-bit computing architecture.
        /// </summary>
        Android = 5,

        /// <summary>
        ///     Mobile versions of iOS (Apple) on 64-bit computing architecture.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Product name.")]
        [SuppressMessage("StyleCop.Naming", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Product name.")]
        iOS = 6,

        /// <summary>
        ///     Versions of WebAssembly (64-bit) on some WASI (WebAssembly System Interface) compliant host program such as a modern web browser.
        /// </summary>
        Web = 7,

        // TODO: tvOS, RaspberryPi, WebAssembly, PlayStation4, PlayStationVita, Switch etc
    }
}
