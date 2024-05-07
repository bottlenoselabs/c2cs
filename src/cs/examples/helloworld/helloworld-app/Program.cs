// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;
using Bindgen.Runtime;
using static my_c_library;

internal static class Program
{
    private static unsafe void Main()
    {
        hw_hello_world();

        var cString1 = (CString)"Hello world from C#!";
        hw_pass_string(cString1);
        Marshal.FreeHGlobal(cString1);

        hw_pass_integers_by_value(65449, -255, 24242);

        ushort a = 65449;
        var b = -255;
        ulong c = 24242;
        hw_pass_integers_by_reference(&a, &b, &c);

#if NET5_0_OR_GREATER
        var functionPointer = new FnPtr_CString_Void(&Callback);
#else
        var functionPointer = new FnPtr_CString_Void(Callback);
#endif
        var cString2 = (CString)"Hello from callback!";
        hw_invoke_callback(functionPointer, cString2);
        Marshal.FreeHGlobal(cString2);
    }

#if NET5_0_OR_GREATER
    [UnmanagedCallersOnly]
#endif
    private static void Callback(CString param)
    {
        // This C# function is called from C

        // Get the string and print it
        var str = param.ToString();
        Console.WriteLine(str);
    }
}
