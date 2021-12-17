using System;
using System.Runtime.InteropServices;

/// <summary>
///     A pointer value type of bytes that represent a string; the C type `char*`.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct CString : IEquatable<CString>
{
    internal readonly nint _pointer;

    /// <summary>
    ///     Gets a value indicating whether this <see cref="CString" /> is a null pointer.
    /// </summary>
    public bool IsNull => _pointer == 0;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CString" /> struct.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    public CString(byte* value)
    {
        _pointer = (nint)value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CString" /> struct.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    public CString(nint value)
    {
        _pointer = value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CString" /> struct.
    /// </summary>
    /// <param name="s">The string value.</param>
    public CString(string s)
    {
        _pointer = CStrings.CString(s);
    }

    /// <summary>
    ///     Performs an explicit conversion from a <see cref="IntPtr" /> to a <see cref="CString" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CString" />.
    /// </returns>
    public static explicit operator CString(nint value)
    {
        return FromIntPtr(value);
    }

    /// <summary>
    ///     Performs an explicit conversion from a <see cref="IntPtr" /> to a <see cref="CString" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CString" />.
    /// </returns>
    public static CString FromIntPtr(nint value)
    {
        return new(value);
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
        return new((nint)value);
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="CString" /> to a <see cref="IntPtr" />.
    /// </summary>
    /// <param name="value">The pointer.</param>
    /// <returns>
    ///     The resulting <see cref="IntPtr" />.
    /// </returns>
    public static implicit operator nint(CString value)
    {
        return value._pointer;
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="CString" /> to a <see cref="IntPtr" />.
    /// </summary>
    /// <param name="value">The pointer.</param>
    /// <returns>
    ///     The resulting <see cref="IntPtr" />.
    /// </returns>
    public static nint ToIntPtr(CString value)
    {
        return value._pointer;
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="CString" /> to a <see cref="string" />.
    /// </summary>
    /// <param name="value">The <see cref="CString" />.</param>
    /// <returns>
    ///     The resulting <see cref="string" />.
    /// </returns>
    public static implicit operator string(CString value)
    {
        return ToString(value);
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="CString" /> to a <see cref="string" />.
    /// </summary>
    /// <param name="value">The <see cref="CString" />.</param>
    /// <returns>
    ///     The resulting <see cref="string" />.
    /// </returns>
    public static string ToString(CString value)
    {
        return CStrings.String(value);
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="string" /> to a <see cref="CString" />.
    /// </summary>
    /// <param name="s">The <see cref="string" />.</param>
    /// <returns>
    ///     The resulting <see cref="CString" />.
    /// </returns>
    public static implicit operator CString(string s)
    {
        return FromString(s);
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="string" /> to a <see cref="CString" />.
    /// </summary>
    /// <param name="s">The <see cref="string" />.</param>
    /// <returns>
    ///     The resulting <see cref="CString" />.
    /// </returns>
    public static CString FromString(string s)
    {
        return CStrings.CString(s);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return CStrings.String(this);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CString value && Equals(value);
    }

    /// <inheritdoc />
    public bool Equals(CString other)
    {
        return _pointer == other._pointer;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _pointer.GetHashCode();
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CString" /> structures are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CString" /> to compare.</param>
    /// <param name="right">The second <see cref="CString" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(CString left, CString right)
    {
        return left._pointer == right._pointer;
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CBool" /> structures are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="CString" /> to compare.</param>
    /// <param name="right">The second <see cref="CString" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(CString left, CString right)
    {
        return !(left == right);
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CString" /> structures are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CString" /> to compare.</param>
    /// <param name="right">The second <see cref="CString" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <c>false</c>.</returns>
    public static bool Equals(CString left, CString right)
    {
        return left._pointer == right._pointer;
    }
}
