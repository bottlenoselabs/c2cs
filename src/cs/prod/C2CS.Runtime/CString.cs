// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

[StructLayout(LayoutKind.Sequential)]
[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
public readonly struct CString
{
    internal readonly IntPtr _value;

    public bool IsNull => _value == IntPtr.Zero;

    public CString(IntPtr value)
    {
        _value = value;
    }

    public CString(string s)
    {
        _value = Runtime.AllocateCString(s);
    }

    public static explicit operator CString(IntPtr ptr)
    {
        return new(ptr);
    }

    public static unsafe implicit operator CString(byte* value)
    {
        return new((IntPtr)value);
    }

    public static implicit operator IntPtr(CString ptr)
    {
        return ptr._value;
    }

    public static implicit operator string(CString ptr)
    {
        return Runtime.AllocateString(ptr);
    }

    public static implicit operator CString(string str)
    {
        return Runtime.AllocateCString(str);
    }

    /// <inheritdoc />
    public override unsafe int GetHashCode()
    {
        return (int)Runtime.djb2((byte*) _value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Runtime.AllocateString(this);
    }
}
