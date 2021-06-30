// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace C2CS
{
    public static unsafe partial class Runtime
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
            var result = Unsafe.ReadUnaligned<T>(source);
            return result;
        }
    }
}
