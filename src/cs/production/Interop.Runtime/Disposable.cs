// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Threading;

namespace Interop.Runtime;

/// <summary>
///     Represents a object instance that has unmanaged resources that can be free-ed, released, or reset.
/// </summary>
public abstract class Disposable : IDisposable
{
    private volatile int _isDisposed;

    /// <summary>
    ///     Gets a value indicating whether the object instance has free-ed, released, or reset unmanaged resources.
    /// </summary>
    /// <remarks><para><see cref="IsDisposed" /> is thread-safe via atomic operations.</para></remarks>
    public bool IsDisposed => _isDisposed == 1;

    /// <summary>
    ///     Performs tasks related to freeing, releasing, or resetting unmanaged resources.
    /// </summary>
#pragma warning disable CA1063
    public void Dispose()
#pragma warning restore CA1063
    {
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
        var isDisposed = Interlocked.CompareExchange(ref _isDisposed, 1, 0);
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
        if (isDisposed == 1)
        {
            return;
        }

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Finalizes an instance of the <see cref="Disposable" /> class.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#pragma warning disable CA1063
    ~Disposable()
#pragma warning restore CA1063
    {
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
        var isDisposed = Interlocked.CompareExchange(ref _isDisposed, 1, 0);
#pragma warning restore CS0420 // A reference to a volatile field will not be treated as volatile
        if (isDisposed == 1)
        {
            return;
        }

        Dispose(false);
    }

    /// <summary>
    ///     Performs tasks related to freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="isDisposing">
    ///     <c>true</c> if <see cref="Dispose()" /> was called explicitly. <c>false</c> if
    ///     <see cref="Dispose()" /> was called implicitly by the garbage collector finalizer.
    /// </param>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    protected abstract void Dispose(bool isDisposing);
}
