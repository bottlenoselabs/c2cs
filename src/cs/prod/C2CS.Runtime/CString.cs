// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace C2CS
{
    [StructLayout(LayoutKind.Sequential)]
    [PublicAPI]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
    public readonly unsafe struct CString
    {
        internal readonly IntPtr _value;

        public bool IsNull => _value == IntPtr.Zero;

        public CString(byte* value)
        {
            _value = (IntPtr) value;
        }

        public CString(IntPtr value)
        {
            _value = value;
        }

        public CString(string s)
        {
            _value = Runtime.CString(s);
        }

        public static explicit operator CString(IntPtr ptr)
        {
            return new(ptr);
        }

        public static implicit operator CString(byte* value)
        {
            return new((IntPtr)value);
        }

        public static implicit operator IntPtr(CString ptr)
        {
            return ptr._value;
        }

        public static implicit operator string(CString ptr)
        {
            return Runtime.String(ptr);
        }

        public static implicit operator CString(string str)
        {
            return Runtime.CString(str);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (int)Runtime.djb2((byte*) _value);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Runtime.String(this);
        }
    }
}
