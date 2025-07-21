// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;

namespace Interop.Runtime;

/// <summary>
///     An allocator that uses a single block of native memory which can be re-used.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="ArenaNativeAllocator" /> can be useful in native interoperability situations when you need to
///         temporarily allocate memory. For example, when calling native functions sometimes memory needs be available
///         for one or more calls but is no longer used after.
///     </para>
/// </remarks>
public sealed unsafe class ArenaNativeAllocator
    : INativeAllocator, IDisposable
{
    private IntPtr _buffer;

    /// <summary>
    ///     Gets the total byte count of the underlying block of native memory.
    /// </summary>
    public int Capacity { get; private set; }

    /// <summary>
    ///     Gets the used byte count of the underlying block of native memory.
    /// </summary>
    public int Used { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ArenaNativeAllocator" /> class.
    /// </summary>
    /// <param name="capacity">The number of bytes to allocate from native memory.</param>
    /// <exception cref="ArgumentOutOfRangeException">The <paramref name="capacity" /> is negative or zero.</exception>
    /// <exception cref="OutOfMemoryException">Allocating <paramref name="capacity" /> of memory failed.</exception>
    public ArenaNativeAllocator(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

#if NET6_0_OR_GREATER
        // malloc
        _buffer = (IntPtr)NativeMemory.AllocZeroed((UIntPtr)capacity);
#else
        _buffer = Marshal.AllocHGlobal(capacity);
        new Span<byte>((void*)_buffer, Capacity).Clear();
#endif
        Capacity = capacity;
        Used = 0;
    }

    /// <summary>
    ///     Frees the the underlying single block of native memory.
    /// </summary>
    public void Dispose()
    {
#if NET6_0_OR_GREATER
        NativeMemory.Free((void*)_buffer);
#else
        Marshal.FreeHGlobal(_buffer);
#endif
        _buffer = IntPtr.Zero;
        Capacity = 0;
        Used = 0;
    }

    /// <summary>
    ///     Uses the next immediate available specified number of bytes from the underlying block of native memory.
    /// </summary>
    /// <param name="byteCount">The number of bytes.</param>
    /// <returns>A pointer to the bytes.</returns>
    /// <exception cref="ObjectDisposedException">The underlying block of native memory is freed.</exception>
    /// <exception cref="InvalidOperationException">The underlying block of native memory is too small.</exception>
    public IntPtr Allocate(int byteCount)
    {
        if (_buffer == IntPtr.Zero)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        if (Used + byteCount > Capacity)
        {
            throw new InvalidOperationException(
                $"Cannot allocate more than {Capacity} bytes with this instance of {nameof(ArenaNativeAllocator)}.");
        }

        var pointer = _buffer + Used;
        Used += byteCount;
        return pointer;
    }

    /// <summary>
    ///     Does nothing. To reclaim the associated memory, call the <see cref="Reset" /> method. To free the associated
    ///     memory call the <see cref="Dispose" /> method instead.
    /// </summary>
    /// <param name="pointer">The pointer to the bytes.</param>
    public void Free(IntPtr pointer)
    {
        // Do nothing
    }

    /// <summary>
    ///     Resets <see cref="Used" /> to zero and clears the entire underlying block of native memory with zeroes.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Any pointers returned from <see cref="Allocate" /> before calling <see cref="Reset" /> must not be used or else
    ///         there is a risk that the pointers point to unexpected bytes of data.
    ///     </para>
    /// </remarks>
    public void Reset()
    {
        if (_buffer == IntPtr.Zero)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        if (Used == 0)
        {
            return;
        }

#if NET6_0_OR_GREATER
        NativeMemory.Clear((void*)_buffer, (UIntPtr)Used);
#else
        new Span<byte>((void*)_buffer, Used).Clear();
#endif
        Used = 0;
    }
}
