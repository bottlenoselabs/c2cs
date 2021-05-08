// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
#if NETCOREAPP
using System.Runtime.CompilerServices;
#endif

public static unsafe partial class NativeTools
{
    public static T MemoryRead<T>(IntPtr address)
    	where T : unmanaged
    {
        var source = (void*) address;

#if NETCOREAPP
        var result = Unsafe.ReadUnaligned<T>(source);
#else
        throw new NotImplementedException();
#endif

        return result;
    }
}
