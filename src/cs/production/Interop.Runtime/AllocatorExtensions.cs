// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace Interop.Runtime;

/// <summary>
///     Provides extension methods for allocating memory using the <see cref="INativeAllocator"/> interface.
/// </summary>
public static unsafe class AllocatorExtensions
{
    /// <summary>
    ///     Allocates memory for a single instance of the specified unmanaged type.
    /// </summary>
    /// <param name="allocator">The allocator used to allocate the memory.</param>
    /// <typeparam name="T">The unmanaged type to allocate memory for.</typeparam>
    /// <returns>A pointer to the allocated memory for the instance of type <typeparamref name="T"/>.</returns>
    public static T* AllocateSingle<T>(this INativeAllocator allocator)
        where T : unmanaged
    {
        var size = sizeof(T);
        return (T*)allocator.Allocate(size);
    }

    /// <summary>
    ///     Allocates a contiguous block of memory for a specified number of elements of type <typeparamref name="T"/>
    ///     and returns it as a pointer to the first element.
    /// </summary>
    /// <param name="allocator">The allocator used to allocate the memory.</param>
    /// <param name="elementCount">The number of elements in the array.</param>
    /// <typeparam name="T">The unmanaged type of the elements to allocate memory for.</typeparam>
    /// <returns>A pointer of type <typeparamref name="T"/> to the first element in the allocated array.</returns>
    public static T* AllocateArray<T>(this INativeAllocator allocator, int elementCount)
        where T : unmanaged
    {
        var size = sizeof(T) * elementCount;
        return (T*)allocator.Allocate(size);
    }

    /// <summary>
    ///     Allocates a contiguous block of memory for a specified number of elements of type <typeparamref name="T"/>
    ///     and returns it as a <see cref="Span{T}"/>.
    /// </summary>
    /// <param name="allocator">The allocator used to allocate the memory.</param>
    /// <param name="elementCount">The number of elements to allocate memory for.</param>
    /// <typeparam name="T">The unmanaged type of the elements to allocate memory for.</typeparam>
    /// <returns>A <see cref="Span{T}"/> representing the allocated memory block.</returns>
    public static Span<T> AllocateSpan<T>(this INativeAllocator allocator, int elementCount)
        where T : unmanaged
    {
        var size = sizeof(T) * elementCount;
        return new Span<T>((void*)allocator.Allocate(size), elementCount);
    }

    /// <summary>
    ///     Allocates memory for a C-style string based on the provided .NET string and returns a <see cref="CString"/>.
    /// </summary>
    /// <param name="allocator">The allocator used to allocate the memory.</param>
    /// <param name="str"> The UTF-16 encoded .NET string to be converted into a null-terminated C string. </param>
    /// <returns> A <see cref="CString"/> representing the allocated C-style string. </returns>
    public static CString AllocateCString(this INativeAllocator allocator, string str)
    {
        return CString.FromString(allocator, str);
    }
}
