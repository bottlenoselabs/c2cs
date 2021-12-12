// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace C2CS
{
    /// <summary>
    ///     A boolean value type with the same memory layout as a <see cref="byte" /> in both managed and unmanaged contexts;
    ///     equivalent to a standard bool found in C/C++/ObjC where <c>0</c> is <c>false</c> and any other value is
    ///     <c>true</c>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [PublicAPI]
    [SuppressMessage(
        "Naming",
        "CA1716",
        MessageId = "Identifiers should not match keywords",
        Justification = "Keyword clash for `CBool` function in Visual Basic .NET, but I don't care about Visual Basic.")]
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
            return CBool.FromBoolean(value);
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
            return Convert.ToBoolean(value);
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
        /// <returns><c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(CBool left, CBool right)
        {
            return left._value == right._value;
        }

        /// <summary>
        ///     Returns a value that indicates whether two specified <see cref="CBool" /> structures are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="CBool" /> to compare.</param>
        /// <param name="right">The second <see cref="CBool" /> to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> and <paramref name="right"/> are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(CBool left, CBool right)
        {
            return !(left == right);
        }

        /// <summary>
        ///     Returns a value that indicates whether two specified <see cref="CBool" /> structures are equal.
        /// </summary>
        /// <param name="left">The first <see cref="CBool" /> to compare.</param>
        /// <param name="right">The second <see cref="CBool" /> to compare.</param>
        /// <returns><c>true</c> if <paramref name="left"/> and <paramref name="right"/> are equal; otherwise, <c>false</c>.</returns>
        public static bool Equals(CBool left, CBool right)
        {
            return left._value == right._value;
        }
    }
}
