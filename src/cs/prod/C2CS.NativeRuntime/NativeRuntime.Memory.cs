// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;
#if NETCOREAPP
using System.Runtime.CompilerServices;
#endif

public static unsafe partial class NativeRuntime
{
    public static void* AllocateMemory(int byteCount)
    {
        var result = Marshal.AllocHGlobal(byteCount);
        return (void*) result;
    }

    public static void FreeMemory(void* pointer)
    {
        Marshal.FreeHGlobal((IntPtr) pointer);
    }

    public static T ReadMemory<T>(IntPtr address)
    	where T : unmanaged
    {
        var source = (void*) address;

#if NETCOREAPP
        var result = Unsafe.ReadUnaligned<T>(source);
#else
        throw new NotImplementedException();
#endif

        return result;
    }
}
