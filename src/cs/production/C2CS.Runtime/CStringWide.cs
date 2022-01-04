using System;
using System.Runtime.InteropServices;

/// <summary>
///     A pointer value type that represents a wide string; C type `wchar_t*`.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct CStringWide : IEquatable<CStringWide>
{
    internal readonly nint _pointer;

    /// <summary>
    ///     Gets a value indicating whether this <see cref="CStringWide" /> is a null pointer.
    /// </summary>
    public bool IsNull => _pointer == 0;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CStringWide" /> struct.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    public CStringWide(byte* value)
    {
        _pointer = (nint)value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CStringWide" /> struct.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    public CStringWide(nint value)
    {
        _pointer = value;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="CStringWide" /> struct.
    /// </summary>
    /// <param name="s">The string value.</param>
    public CStringWide(string s)
    {
        _pointer = CStrings.CStringWide(s);
    }

    /// <summary>
    ///     Performs an explicit conversion from a <see cref="IntPtr" /> to a <see cref="CStringWide" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CStringWide" />.
    /// </returns>
    public static explicit operator CStringWide(nint value)
    {
        return FromIntPtr(value);
    }

    /// <summary>
    ///     Performs an explicit conversion from a <see cref="IntPtr" /> to a <see cref="CStringWide" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CStringWide" />.
    /// </returns>
    public static CStringWide FromIntPtr(nint value)
    {
        return new CStringWide(value);
    }

    /// <summary>
    ///     Performs an implicit conversion from a byte pointer to a <see cref="CStringWide" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CStringWide" />.
    /// </returns>
    public static implicit operator CStringWide(byte* value)
    {
        return From(value);
    }

    /// <summary>
    ///     Performs an implicit conversion from a byte pointer to a <see cref="CStringWide" />.
    /// </summary>
    /// <param name="value">The pointer value.</param>
    /// <returns>
    ///     The resulting <see cref="CStringWide" />.
    /// </returns>
    public static CStringWide From(byte* value)
    {
        return new CStringWide((nint)value);
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="CStringWide" /> to a <see cref="IntPtr" />.
    /// </summary>
    /// <param name="value">The pointer.</param>
    /// <returns>
    ///     The resulting <see cref="IntPtr" />.
    /// </returns>
    public static implicit operator nint(CStringWide value)
    {
        return value._pointer;
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="CStringWide" /> to a <see cref="IntPtr" />.
    /// </summary>
    /// <param name="value">The pointer.</param>
    /// <returns>
    ///     The resulting <see cref="IntPtr" />.
    /// </returns>
    public static nint ToIntPtr(CStringWide value)
    {
        return value._pointer;
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="CStringWide" /> to a <see cref="string" />.
    /// </summary>
    /// <param name="value">The <see cref="CStringWide" />.</param>
    /// <returns>
    ///     The resulting <see cref="string" />.
    /// </returns>
    public static implicit operator string(CStringWide value)
    {
        return ToString(value);
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="CStringWide" /> to a <see cref="string" />.
    /// </summary>
    /// <param name="value">The <see cref="CStringWide" />.</param>
    /// <returns>
    ///     The resulting <see cref="string" />.
    /// </returns>
    public static string ToString(CStringWide value)
    {
        return CStrings.StringWide(value);
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="string" /> to a <see cref="CStringWide" />.
    /// </summary>
    /// <param name="s">The <see cref="string" />.</param>
    /// <returns>
    ///     The resulting <see cref="CStringWide" />.
    /// </returns>
    public static implicit operator CStringWide(string s)
    {
        return FromString(s);
    }

    /// <summary>
    ///     Performs an implicit conversion from a <see cref="string" /> to a <see cref="CStringWide" />.
    /// </summary>
    /// <param name="s">The <see cref="string" />.</param>
    /// <returns>
    ///     The resulting <see cref="CStringWide" />.
    /// </returns>
    public static CStringWide FromString(string s)
    {
        return CStrings.CStringWide(s);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return CStrings.StringWide(this);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CStringWide value && Equals(value);
    }

    /// <inheritdoc />
    public bool Equals(CStringWide other)
    {
        return _pointer == other._pointer;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _pointer.GetHashCode();
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CStringWide" /> structures are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CStringWide" /> to compare.</param>
    /// <param name="right">The second <see cref="CStringWide" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(CStringWide left, CStringWide right)
    {
        return left._pointer == right._pointer;
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CBool" /> structures are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="CStringWide" /> to compare.</param>
    /// <param name="right">The second <see cref="CStringWide" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(CStringWide left, CStringWide right)
    {
        return !(left == right);
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CStringWide" /> structures are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CStringWide" /> to compare.</param>
    /// <param name="right">The second <see cref="CStringWide" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <c>false</c>.</returns>
    public static bool Equals(CStringWide left, CStringWide right)
    {
        return left._pointer == right._pointer;
    }
}
