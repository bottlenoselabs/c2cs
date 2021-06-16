// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

/// <summary>
///     A socket address of various sizes.
/// </summary>
[StructLayout(LayoutKind.Sequential)] // size = 16
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
public unsafe struct sockaddr
{
    /// <summary>
    ///     The number of bytes that are actually to be stored with <see cref="sa_data" />.
    /// </summary>
    /// <remarks>
    ///     On <see cref="RuntimePlatform.Windows" />, <see cref="sa_len" /> is <b>not</b> used and thus is always <c>0x0</c>.
    /// </remarks>
    public byte sa_len;

    /// <summary>
    ///     The socket address family.
    /// </summary>
    public sa_family_t sa_family;

    /// <summary>
    ///     The socket address data.
    /// </summary>
    public fixed sbyte sa_data[14];
}
