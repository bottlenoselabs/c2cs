// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

/// <summary>
///     Process identifier.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
public struct pid_t
{
    /// <summary>
    ///     The identifier value.
    /// </summary>
    public uint Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator uint(pid_t value) => value.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator pid_t(uint value) => new() {Value = value};
}
