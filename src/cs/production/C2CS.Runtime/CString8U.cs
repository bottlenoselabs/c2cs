// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace C2CS
{
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global", Justification = "Public API.")]
    public readonly unsafe struct CString8U
    {
        internal readonly IntPtr _value;

        public bool IsNull => _value == IntPtr.Zero;

        public CString8U(byte* value)
        {
            _value = (IntPtr) value;
        }

        public CString8U(IntPtr value)
        {
            _value = value;
        }

        public CString8U(string s)
        {
            _value = Runtime.CString8U(s);
        }

        public static explicit operator CString8U(IntPtr ptr)
        {
            return new(ptr);
        }

        public static implicit operator CString8U(byte* value)
        {
            return new((IntPtr)value);
        }

        public static implicit operator IntPtr(CString8U ptr)
        {
            return ptr._value;
        }

        public static implicit operator string(CString8U ptr)
        {
            return Runtime.String8U(ptr);
        }

        public static implicit operator CString8U(string str)
        {
            return Runtime.CString8U(str);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)Runtime.djb2((byte*) _value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Runtime.String8U(this);
        }
    }
}
