// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Threading;

#if NETCOREAPP
using System.Runtime.CompilerServices;
#endif

public struct CLibrary
{
    private IntPtr _handle;

    public static CLibrary? Load(string libraryFilePath)
    {
        var handle = NativeTools.LibraryLoad(libraryFilePath);
        if (handle == IntPtr.Zero)
        {
            return null;
        }

#if NETCOREAPP
        Unsafe.SkipInit<CLibrary>(out var library);
#else
        var library = default(CLibrary);
#endif
        library._handle = handle;

        return library;
    }

    public void Free()
    {
        var handle = Interlocked.Exchange(ref _handle, IntPtr.Zero);
        if (handle == IntPtr.Zero)
        {
            return;
        }

        NativeTools.LibraryUnload(handle);
    }

    public IntPtr? GetExport(string name)
    {
        var address = NativeTools.LibraryGetExport(_handle, name);
        if (address == IntPtr.Zero)
        {
            return null;
        }

        return address;
    }
}
