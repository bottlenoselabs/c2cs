using System;
using System.Runtime.InteropServices;

/// <summary>
///     A boolean value type with the same memory layout as a <see cref="byte" /> in both managed and unmanaged contexts;
///     equivalent to a standard bool found in C/C++/ObjC where <c>0</c> is <c>false</c> and any other value is
///     <c>true</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct CBool : IEquatable<CBool>
{
    private readonly byte _value;

    private CBool(bool value)
    {
        _value = Convert.ToByte(value);
    }

    /// <summary>
    ///     Converts the specified <see cref="bool" /> to a <see cref="CBool" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A <see cref="CBool" />.</returns>
    public static implicit operator CBool(bool value)
    {
        return FromBoolean(value);
    }

    /// <summary>
    ///     Converts the specified <see cref="bool" /> to a <see cref="CBool" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A <see cref="CBool" />.</returns>
    public static CBool FromBoolean(bool value)
    {
        return new CBool(value);
    }

    /// <summary>
    ///     Converts the specified <see cref="CBool" /> to a <see cref="bool" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A <see cref="bool" />.</returns>
    public static implicit operator bool(CBool value)
    {
        return ToBoolean(value);
    }

    /// <summary>
    ///     Converts the specified <see cref="CBool" /> to a <see cref="bool" />.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A <see cref="bool" />.</returns>
    public static bool ToBoolean(CBool value)
    {
        return Convert.ToBoolean(value._value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return ToBoolean(this).ToString();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CBool b && Equals(b);
    }

    /// <inheritdoc />
    public bool Equals(CBool other)
    {
        return _value == other._value;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CBool" /> structures are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CBool" /> to compare.</param>
    /// <param name="right">The second <see cref="CBool" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(CBool left, CBool right)
    {
        return left._value == right._value;
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CBool" /> structures are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="CBool" /> to compare.</param>
    /// <param name="right">The second <see cref="CBool" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(CBool left, CBool right)
    {
        return !(left == right);
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CBool" /> structures are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CBool" /> to compare.</param>
    /// <param name="right">The second <see cref="CBool" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <c>false</c>.</returns>
    public static bool Equals(CBool left, CBool right)
    {
        return left._value == right._value;
    }
}
