// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace Interop.Runtime;

/// <summary>
///     Represents an object instance that is associated with an unmanaged handle.
/// </summary>
public abstract class NativeHandle : Disposable
{
    /// <summary>
    ///     Gets the unmanaged handle associated with the object instance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Handle" /> is <see cref="IntPtr.Zero" /> when <see cref="Disposable.IsDisposed" /> is
    ///         <c>true</c>.
    ///     </para>
    /// </remarks>
    public IntPtr Handle { get; internal set; }

    internal NativeHandle(IntPtr handle)
    {
        Handle = handle;
    }

    /// <inheritdoc />
    protected override void Dispose(bool isDisposing)
    {
        Handle = IntPtr.Zero;
    }
}
