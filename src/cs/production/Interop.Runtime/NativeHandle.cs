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
    ///     Gets or sets the unmanaged handle associated with the object instance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="Handle" /> is <see cref="IntPtr.Zero" /> when <see cref="Disposable.IsDisposed" /> is
    ///         <c>true</c>.
    ///     </para>
    /// </remarks>
    public IntPtr Handle { get; protected set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NativeHandle" /> class.
    /// </summary>
    protected NativeHandle()
    {
        Handle = IntPtr.Zero;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NativeHandle" /> class using the specified pointer as the
    ///     handle.
    /// </summary>
    /// <param name="handle">The native handle.</param>
    protected NativeHandle(IntPtr handle)
    {
        Handle = handle;
    }

    /// <inheritdoc />
    protected override void Dispose(bool isDisposing)
    {
        Handle = IntPtr.Zero;
    }
}
