// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;

namespace C2CS
{
    /// <summary>
    ///     Socket address families.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global", Justification = "Public API.")]
    public enum sa_family_t : byte
    {
        /// <summary>
        ///     Unspecified.
        /// </summary>
        AF_UNSPEC = 0,

        /// <summary>
        ///     IP (Internet Protocol) version 4. Used for both UDP (User Datagram Protocol) and TCP (Transmission Control
        ///     Protocol) transport layers.
        /// </summary>
        AF_INET = 2,

        // TODO: Figure out how to deal with this without using compiler directives
        /*/// <summary>
        ///     IP (Internet Protocol) version 6. Used for both UDP (User Datagram Protocol) and TCP (Transmission Control
        ///     Protocol) transport layers.
        /// </summary>
        AF_INET6 =
#if WINDOWS
    23
#elif APPLE
    30
#elif LINUX
    10
#else
    0
#endif*/
    }
}
