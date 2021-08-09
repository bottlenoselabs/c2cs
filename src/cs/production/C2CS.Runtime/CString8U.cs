// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace C2CS
{
    /// <summary>
    ///     A pointer value type; represents the 8-bit C type `char*`.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global", Justification = "Public API.")]
    public readonly unsafe struct CString8U
    {
        internal readonly IntPtr _ptr;

        /// <summary>
        ///     Gets a <see cref="bool" /> value indicating whether this <see cref="CString8U" /> is a null pointer.
        /// </summary>
        public bool IsNull => _ptr == IntPtr.Zero;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CString8U" /> struct.
        /// </summary>
        /// <param name="ptr">The pointer value.</param>
        public CString8U(byte* ptr)
        {
            _ptr = (IntPtr)ptr;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CString8U" /> struct.
        /// </summary>
        /// <param name="ptr">The pointer value.</param>
        public CString8U(IntPtr ptr)
        {
            _ptr = ptr;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CString8U" /> struct.
        /// </summary>
        /// <param name="s">The string.</param>
        public CString8U(string s)
        {
            _ptr = Runtime.CString8U(s);
        }

        /// <summary>
        ///     Performs an explicit conversion from a <see cref="IntPtr" /> to a <see cref="CString8U" />.
        /// </summary>
        /// <param name="ptr">The pointer.</param>
        /// <returns>
        ///     The resulting <see cref="CString8U" />.
        /// </returns>
        public static explicit operator CString8U(IntPtr ptr)
        {
            return new CString8U(ptr);
        }

        /// <summary>
        ///     Performs an implicit conversion from a byte pointer to a <see cref="CString8U" />.
        /// </summary>
        /// <param name="ptr">The pointer.</param>
        /// <returns>
        ///     The resulting <see cref="CString8U" />.
        /// </returns>
        public static implicit operator CString8U(byte* ptr)
        {
            return new CString8U((IntPtr)ptr);
        }

        /// <summary>
        ///     Performs an implicit conversion from a <see cref="CString8U" /> to a <see cref="IntPtr" />.
        /// </summary>
        /// <param name="ptr">The <see cref="CString8U" />.</param>
        /// <returns>
        ///     The resulting <see cref="IntPtr" />.
        /// </returns>
        public static implicit operator IntPtr(CString8U ptr)
        {
            return ptr._ptr;
        }

        /// <summary>
        ///     Performs an implicit conversion from a <see cref="CString8U" /> to a <see cref="string" />.
        /// </summary>
        /// <param name="value">The <see cref="CString8U" />.</param>
        /// <returns>
        ///     The resulting <see cref="string" />.
        /// </returns>
        public static implicit operator string(CString8U value)
        {
            return Runtime.String8U(value);
        }

        /// <summary>
        ///     Performs an implicit conversion from a <see cref="string" /> to a <see cref="CString8U" />.
        /// </summary>
        /// <param name="s">The <see cref="string" />.</param>
        /// <returns>
        ///     The resulting <see cref="CString8U" />.
        /// </returns>
        public static implicit operator CString8U(string s)
        {
            return Runtime.CString8U(s);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)Runtime.djb2((byte*)_ptr);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Runtime.String8U(this);
        }
    }
}
