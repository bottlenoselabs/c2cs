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
public readonly struct AnsiStringPtr
{
    internal readonly IntPtr _value;

    public bool IsNull => _value == IntPtr.Zero;

    public AnsiStringPtr(IntPtr value)
    {
        _value = value;
    }

    public AnsiStringPtr(string s)
    {
        _value = NativeRuntime.AllocateCString(s);
    }

    public static explicit operator AnsiStringPtr(IntPtr ptr)
    {
        return new(ptr);
    }

    public static unsafe implicit operator AnsiStringPtr(byte* value)
    {
        return new((IntPtr)value);
    }

    public static implicit operator IntPtr(AnsiStringPtr ptr)
    {
        return ptr._value;
    }

    public static implicit operator string(AnsiStringPtr ptr)
    {
        return NativeRuntime.AllocateString(ptr);
    }

    public static implicit operator AnsiStringPtr(string str)
    {
        return NativeRuntime.AllocateCString(str);
    }

    /// <inheritdoc />
    public override unsafe int GetHashCode()
    {
        return (int)NativeRuntime.djb2((byte*) _value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return NativeRuntime.AllocateString(this);
    }
}
