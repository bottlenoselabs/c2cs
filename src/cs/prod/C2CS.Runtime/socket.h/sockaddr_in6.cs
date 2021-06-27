// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace C2CS
{
    /// <summary>
    ///     An IP (Internet Protocol) version 6 socket address.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)] // size = 26
    [PublicAPI]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
    public unsafe struct sockaddr_in6
    {
        /// <summary>
        ///     The number of bytes for the data (<see cref="sin6_port" /> + <see cref="sin6_flowinfo" /> +
        ///     <see cref="sin6_addr" /> + <see cref="sin6_scope_id" />).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         On <see cref="RuntimePlatform.Windows" />, <see cref="sin6_len" /> is <b>not</b> used and thus is always
        ///         <c>0x0</c>.
        ///     </para>
        ///     <para>
        ///         The value of <see cref="sin6_len" /> should be equal to <c>0x0</c> on
        ///         <see cref="RuntimePlatform.Windows" /> and <c>24</c> on every other platform.
        ///     </para>
        /// </remarks>
        public byte sin6_len;

        /// <summary>
        ///     The IP (Internet Protocol) v6 address family.
        /// </summary>
        /// <remarks>
        ///     The value of <see cref="sin6_family" /> should always be equal to <see cref="sa_family_t.AF_INET6" />.
        /// </remarks>
        public byte sin6_family;

        /// <summary>
        ///     The IP (Internet Protocol) port.
        /// </summary>
        /// <remarks>
        ///     The value of <see cref="sin6_port" /> is encoded in TCP/IP byte order (big-endian).
        /// </remarks>
        public ushort sin6_port;

        /// <summary>
        ///     The IP (Internet Protocol) traffic class and the flow label.
        /// </summary>
        /// <remarks>
        ///     The value of <see cref="sin6_flowinfo" /> is encoded in TCP/IP byte order (big-endian).
        /// </remarks>
        public uint sin6_flowinfo;

        /// <summary>
        ///     The IP (Internet Protocol) v6 address.
        /// </summary>
        /// <remarks>
        ///     The value of <see cref="sin6_addr" /> is encoded in TCP/IP byte order (big-endian).
        /// </remarks>
        public fixed byte sin6_addr[16];

        /// <summary>
        ///     The IP (Internet Protocol) interface identifier.
        /// </summary>
        /// <remarks>
        ///     The value of <see cref="sin6_scope_id" /> is encoded in TCP/IP byte order (big-endian).
        /// </remarks>
        public uint sin6_scope_id;
    }
}
