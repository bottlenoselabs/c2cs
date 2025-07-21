// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace Interop.Runtime;

/// <summary>
///     Defines methods for allocating and freeing native memory.
/// </summary>
public interface INativeAllocator
{
    /// <summary>
    ///     Attempts to allocate a block of memory.
    /// </summary>
    /// <param name="byteCount">The number of bytes to allocate.</param>
    /// <returns>If the memory was allocated successfully, a pointer to the allocated block of memory; otherwise, <c>null</c>.</returns>
    IntPtr Allocate(int byteCount);

    /// <summary>
    ///     Frees a previously specified block of allocated memory.
    /// </summary>
    /// <param name="pointer">The pointer to the block of memory.</param>
    void Free(IntPtr pointer);

    /// <summary>
    ///     Resets the associated memory.
    /// </summary>
    void Reset();
}
