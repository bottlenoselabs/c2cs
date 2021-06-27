// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace C2CS
{
    /// <summary>
    ///     An IP (Internet Protocol) version 4 socket address.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)] // size = 16
    [PublicAPI]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
    public unsafe struct sockaddr_in
    {
        /// <summary>
        ///     The number of bytes for the data (<see cref="sin_port" /> + <see cref="sin_addr" />).
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         On <see cref="RuntimePlatform.Windows" />, <see cref="sin_len" /> is <b>not</b> used and thus is always
        ///         <c>0x0</c>.
        ///     </para>
        ///     <para>
        ///         The value of <see cref="sin_len" /> should always be equal to <c>0x0</c> on
        ///         <see cref="RuntimePlatform.Windows" /> and <c>6</c> on every other platform.
        ///     </para>
        /// </remarks>
        public byte sin_len;

        /// <summary>
        ///     The IP (Internet Protocol) v4 address family.
        /// </summary>
        /// <remarks>
        ///     The value of <see cref="sin_family" /> should always be equal to <see cref="sa_family_t.AF_INET" />.
        /// </remarks>
        public sa_family_t sin_family;

        /// <summary>
        ///     The IP (Internet Protocol) port.
        /// </summary>
        /// <remarks>
        ///     The value of <see cref="sin_port" /> is encoded in TCP/IP byte order (big-endian).
        /// </remarks>
        public ushort sin_port;

        /// <summary>
        ///     The IP (Internet Protocol) v4 address.
        /// </summary>
        /// <remarks>
        ///     The value of <see cref="sin_port" /> is encoded in TCP/IP byte order (big-endian).
        /// </remarks>
        public uint sin_addr;

        /// <summary>
        ///     Unused. All these bytes should be <c>0x0</c>.
        /// </summary>
        public fixed byte sin_zero[8]; // char [8]
    }
}
