// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace C2CS;

public static unsafe partial class Runtime
{
    /// <summary>
    ///     C equivalent of `malloc`; allocates a specified size of memory in bytes.
    /// </summary>
    /// <param name="byteCount">The numbers of bytes to allocate.</param>
    /// <returns>A pointer to the allocated memory.</returns>
    public static void* AllocateMemory(int byteCount)
    {
        var result = Marshal.AllocHGlobal(byteCount);
        return (void*)result;
    }

    /// <summary>
    ///     C equivalent of `free`; deallocates previously allocated memory.
    /// </summary>
    /// <param name="pointer">The pointer to the allocated memory.</param>
    public static void FreeMemory(void* pointer)
    {
        Marshal.FreeHGlobal((nint)pointer);
    }

    /// <summary>
    ///     Reads a blittable value type of <see cref="Runtime.ReadMemory{T}" /> from the memory address.
    /// </summary>
    /// <param name="address">The memory address.</param>
    /// <typeparam name="T">The blittable value type.</typeparam>
    /// <returns>The read blittable value type <see cref="Runtime.ReadMemory{T}" /> from memory.</returns>
    public static T ReadMemory<T>(nint address)
        where T : unmanaged
    {
        var source = (void*)address;
        var result = Unsafe.ReadUnaligned<T>(source);
        return result;
    }
}
