// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
#if NETCOREAPP
using System.Runtime.CompilerServices;

#endif

public static partial class NativeTools
{
    public static IntPtr LibraryLoad(string libraryFilePath)
    {
        if (string.IsNullOrEmpty(libraryFilePath))
        {
            return IntPtr.Zero;
        }

#if NETCOREAPP
        Unsafe.SkipInit<IntPtr>(out var handle);
#else
        var handle = default(IntPtr.Zero);
#endif

#if NETCOREAPP
        if (!NativeLibrary.TryLoad(libraryFilePath, out handle))
        {
            return IntPtr.Zero;
        }
#else
        throw new NotImplementedException();
#endif

        return handle;
    }

    public static void LibraryUnload(IntPtr libraryHandle)
    {
#if NETCOREAPP
        NativeLibrary.Free(libraryHandle);
#else
        throw new NotImplementedException();
#endif
    }

    public static IntPtr LibraryGetExport(IntPtr libraryHandle, string name)
    {
        if (libraryHandle == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

#if NETCOREAPP
        Unsafe.SkipInit<IntPtr>(out var address);
#else
        var address = default(IntPtr.Zero);
#endif

#if NETCOREAPP
        if (!NativeLibrary.TryGetExport(libraryHandle, name, out address))
        {
            return IntPtr.Zero;
        }
#else
        throw new NotImplementedException();
#endif

        return address;
    }
}
