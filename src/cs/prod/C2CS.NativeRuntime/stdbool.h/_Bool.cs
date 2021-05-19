// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

/// <summary>
///     A boolean value type with the same memory layout as a <see cref="byte" /> in both managed and unmanaged contexts;
///     equivalent to a standard bool found in C/C++/ObjC where <c>0</c> is <c>false</c> and any other value is
///     <c>true</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
public readonly struct _Bool
{
    private readonly byte _value;

    private _Bool(bool value)
    {
        _value = Convert.ToByte(value);
    }

    /// <summary>
    ///     Converts the specified <see cref="bool" /> to a <see cref="_Bool" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A <see cref="_Bool" />.</returns>
    public static implicit operator _Bool(bool value)
    {
        return new(value);
    }

    /// <summary>
    ///     Converts the specified <see cref="_Bool" /> to a <see cref="bool" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A <see cref="bool" />.</returns>
    public static implicit operator bool(_Bool value)
    {
        return Convert.ToBoolean(value._value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Convert.ToBoolean(_value).ToString();
    }
}
