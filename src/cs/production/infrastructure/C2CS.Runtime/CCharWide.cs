using System;
using System.Globalization;
using System.Runtime.InteropServices;

/// <summary>
///     A value type with the memory layout of a <c>wchar_t</c> in an unmanaged context. The memory layout in a
///     managed context depends on the operating system or otherwise on preprocessor directives defines.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct CCharWide : IEquatable<CCharWide>
{
#if SIZEOF_WCHAR_T_1
    private readonly byte _value;
#elif SIZEOF_WCHAR_T_2
    private readonly ushort _value;
#elif SIZEOF_WCHAR_T_4
    private readonly uint _value;
#else
    private readonly ushort _value;
#endif

    private CCharWide(byte value)
    {
#if SIZEOF_WCHAR_T_1
        _value = Convert.ToByte(value);
#elif SIZEOF_WCHAR_T_2
        _value = Convert.ToUInt16(value);
#elif SIZEOF_WCHAR_T_4
        _value = Convert.ToUInt32(value);
#else
        _value = Convert.ToUInt16(value);
#endif
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return _value.ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is CCharWide value && Equals(value);
    }

    /// <inheritdoc />
    public bool Equals(CCharWide other)
    {
        return _value == other._value;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CCharWide" /> structures are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CCharWide" /> to compare.</param>
    /// <param name="right">The second <see cref="CCharWide" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(CCharWide left, CCharWide right)
    {
        return left._value == right._value;
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CCharWide" /> structures are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="CCharWide" /> to compare.</param>
    /// <param name="right">The second <see cref="CCharWide" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <c>false</c>.</returns>
    public static bool operator !=(CCharWide left, CCharWide right)
    {
        return !(left == right);
    }

    /// <summary>
    ///     Returns a value that indicates whether two specified <see cref="CCharWide" /> structures are equal.
    /// </summary>
    /// <param name="left">The first <see cref="CCharWide" /> to compare.</param>
    /// <param name="right">The second <see cref="CCharWide" /> to compare.</param>
    /// <returns><c>true</c> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <c>false</c>.</returns>
    public static bool Equals(CCharWide left, CCharWide right)
    {
        return left._value == right._value;
    }
}
