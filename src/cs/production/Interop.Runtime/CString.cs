// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;

namespace Interop.Runtime;

/// <summary>
///     A pointer value type of bytes that represent a string; the C type `char*`.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct CString : IEquatable<CString>, IDisposable
{
    /// <summary>
    ///     The pointer.
    /// </summary>
    public readonly IntPtr Pointer;

    /// <summary>
    ///     Gets a value indicating whether this <see cref="CString" /> is a null pointer.
    /// </summary>
    public bool IsNull => Pointer == IntPtr.Zero;

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    /// <summary>
    ///     Initializes a new instance of the <see cref="CString" /> struct.
    /// </summary>
    /// <param name="value">The span.</param>
    public CString(ReadOnlySpan<byte> value)
    {
#pragma warning disable CS8500
        fixed (byte* pointer = value)
        {
            Pointer = (IntPtr)pointer;
        }
#pragma warning restore CS8500
    }
#endif

    /// <summary>
    ///     Initializes a new instance of the <see cref="CString" /> struct.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    public CString(byte* value)
    {
        Pointer = (IntPtr)value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CString" /> struct.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    public CString(IntPtr value)
    {
        Pointer = value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CString" /> struct.
    /// </summary>
    /// <param name="s">The string value.</param>
    public CString(string s)
    {
        Pointer = FromString(s).Pointer;
    }

    /// <summary>
    ///     Attempts to free the memory pointed by the <see cref="CString "/>.
    /// </summary>
    public void Dispose()
    {
        Marshal.FreeHGlobal(Pointer);
    }

    /// <summary>
    ///     Performs an explicit conversion from an <see cref="IntPtr" /> to a <see cref="CString" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CString" />.
    /// </returns>
    public static explicit operator CString(IntPtr value)
    {
        return FromIntPtr(value);
    }

    /// <summary>
    ///     Performs a conversion from an <see cref="IntPtr" /> to a <see cref="CString" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CString" />.
    /// </returns>
    public static CString FromIntPtr(IntPtr value)
    {
        return new CString(value);
    }

    /// <summary>
    ///     Performs an implicit conversion from a byte pointer to a <see cref="CString" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CString" />.
    /// </returns>
    public static implicit operator CString(byte* value)
    {
        return From(value);
    }

    /// <summary>
    ///     Performs an implicit conversion from a byte pointer to a <see cref="CString" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CString" />.
    /// </returns>
    public static CString From(byte* value)
    {
        return new CString((IntPtr)value);
    }

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
    /// <summary>
    ///     Performs an explicit conversion from a <see cref="ReadOnlySpan{T}" /> to a <see cref="CString" />.
    /// </summary>
    /// <param name="value">The pointer.</param>
    /// <returns>
    ///     The resulting <see cref="IntPtr" />.
    /// </returns>
    public static explicit operator CString(ReadOnlySpan<byte> value)
    {
        return new CString(value);
    }

    /// <summary>
    ///     Performs a conversion from a <see cref="ReadOnlySpan{T}" /> to a <see cref="CString" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CString" />.
    /// </returns>
    public static CString FromReadOnlySpan(ReadOnlySpan<byte> value)
    {
        return new CString(value);
    }
#endif

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="CString" /> to a <see cref="IntPtr" />.
    /// </summary>
    /// <param name="value">The pointer.</param>
    /// <returns>
    ///     The resulting <see cref="IntPtr" />.
    /// </returns>
    public static implicit operator IntPtr(CString value)
    {
        return value.Pointer;
    }

    /// <summary>
    ///     Performs a conversion from a <see cref="CString" /> to a <see cref="IntPtr" />.
    /// </summary>
    /// <param name="value">The pointer.</param>
    /// <returns>
    ///     The resulting <see cref="IntPtr" />.
    /// </returns>
    public static IntPtr ToIntPtr(CString value)
    {
        return value.Pointer;
    }

    /// <summary>
    ///     Performs an explicit conversion from a <see cref="CString" /> to a <see cref="string" />.
    /// </summary>
    /// <param name="value">The <see cref="CString" />.</param>
    /// <returns>
    ///     The resulting <see cref="string" />.
    /// </returns>
    public static explicit operator string(CString value)
    {
        return ToString(value);
    }

    /// <summary>
    ///     Converts a C style string (ANSI or UTF-8) of type `char` (one dimensional byte array
    ///     terminated by a <c>0x0</c>) to a UTF-16 <see cref="string" /> by allocating and copying.
    /// </summary>
    /// <param name="value">A pointer to the C string.</param>
    /// <returns>A <see cref="string" /> equivalent of <paramref name="value" />.</returns>
    public static string ToString(CString value)
    {
        if (value.IsNull)
        {
            return string.Empty;
        }

        // calls ASM/C/C++ functions to calculate length and then "FastAllocate" the string with the GC
        // https://mattwarren.org/2016/05/31/Strings-and-the-CLR-a-Special-Relationship/
        var result = Marshal.PtrToStringAnsi(value.Pointer);

        if (string.IsNullOrEmpty(result))
        {
            return string.Empty;
        }

        return result;
    }

    /// <summary>
    ///     Performs an explicit conversion from a <see cref="string" /> to a <see cref="CString" />.
    /// </summary>
    /// <param name="s">The <see cref="string" />.</param>
    /// <returns>
    ///     The resulting <see cref="CString" />.
    /// </returns>
    public static explicit operator CString(string s)
    {
        return FromString(s);
    }

    /// <summary>
    ///     Converts a UTF-16 <see cref="string" /> to a C style string (one dimensional byte array terminated by a
    ///     <c>0x0</c>) by allocating and copying.
    /// </summary>
    /// <param name="str">The <see cref="string" />.</param>
    /// <returns>A C string pointer.</returns>
    public static CString FromString(string str)
    {
        var pointer = Marshal.StringToHGlobalAnsi(str);
        return new CString(pointer);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToString(this);
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is CString value && Equals(value);
    }

    /// <inheritdoc />
    public bool Equals(CString other)
    {
        return Pointer == other.Pointer;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Pointer.GetHashCode();
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CString" /> structures are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CString" /> to compare.</param>
    /// <param name="right">The second <see cref="CString" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(CString left, CString right)
    {
        return left.Pointer == right.Pointer;
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CBool" /> structures are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="CString" /> to compare.</param>
    /// <param name="right">The second <see cref="CString" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(CString left, CString right)
    {
        return !(left == right);
    }
}
