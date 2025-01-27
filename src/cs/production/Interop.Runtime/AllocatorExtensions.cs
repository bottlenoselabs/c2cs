// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace Interop.Runtime;

public static unsafe class AllocatorExtensions
{
    public static T* AllocateSingle<T>(this INativeAllocator allocator)
        where T : unmanaged
    {
        var size = sizeof(T);
        return (T*)allocator.Allocate(size);
    }

    public static T* AllocateArray<T>(this INativeAllocator allocator, int elementCount)
        where T : unmanaged
    {
        var size = sizeof(T) * elementCount;
        return (T*)allocator.Allocate(size);
    }

    public static Span<T> AllocateSpan<T>(this INativeAllocator allocator, int elementCount)
        where T : unmanaged
    {
        var size = sizeof(T) * elementCount;
        return new Span<T>((void*)allocator.Allocate(size), elementCount);
    }

    public static CString AllocateCString(this INativeAllocator allocator, string str)
    {
        return CString.FromString(allocator, str);
    }
}
