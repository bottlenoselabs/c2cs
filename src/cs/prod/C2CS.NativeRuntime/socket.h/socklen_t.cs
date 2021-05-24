// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

// According to Linus Torvalds, `socklen_t` is always a unsigned 32-bit integer: https://ipfs.io/ipfs/QmdA5WkDNALetBn4iFeSepHjdLGJdxPBwZyY47ir1bZGAK/comp/linux/socklen_t.html
// 1. Is this not the case? Is there computer architectures that exist to which this is not true?
// 2. If yes, does it matter for .NET?
// If you have evidence to the contrary let me know!

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

/// <summary>
///     Unsigned integer type for sockets.
/// </summary>
[StructLayout(LayoutKind.Sequential)] // size = 4
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
public struct socklen_t
{
    /// <summary>
    ///     The underlying value.
    /// </summary>
    public uint Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(socklen_t value) => value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator socklen_t(uint value) => new() {Value = value};
}
