// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace C2CS
{
    /// <summary>
    ///     Socket address families.
    /// </summary>
    [PublicAPI]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
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

        /// <summary>
        ///     IP (Internet Protocol) version 6. Used for both UDP (User Datagram Protocol) and TCP (Transmission Control
        ///     Protocol) transport layers.
        /// </summary>
#if WINDOWS
    AF_INET6 = 23
#elif APPLE
    AF_INET6 = 30
#elif LINUX
    AF_INET6 = 10
#endif
    }
}
