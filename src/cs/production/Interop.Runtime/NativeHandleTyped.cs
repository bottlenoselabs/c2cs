// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace Interop.Runtime;

/// <summary>
///     Represents an object instance that is associated with a typed unmanaged handle.
/// </summary>
/// <typeparam name="T">The type of pointer.</typeparam>
public abstract unsafe class NativeHandleTyped<T> : NativeHandle
    where T : unmanaged
{
    /// <summary>
    ///     Gets or sets the unmanaged handle associated with the object instance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="HandleTyped" /> is <c>null</c> when <see cref="Disposable.IsDisposed" /> is
    ///         <c>true</c>.
    ///     </para>
    /// </remarks>
    public T* HandleTyped { get; protected set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NativeHandleTyped{T}" /> class.
    /// </summary>
    protected NativeHandleTyped()
    {
        HandleTyped = null;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NativeHandleTyped{T}" /> class using the specified pointer as
    ///     the handle.
    /// </summary>
    /// <param name="handle">The native handle.</param>
    protected NativeHandleTyped(T* handle)
        : base((IntPtr)handle)
    {
        HandleTyped = handle;
    }

    /// <inheritdoc />
    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);
        HandleTyped = null;
    }
}
