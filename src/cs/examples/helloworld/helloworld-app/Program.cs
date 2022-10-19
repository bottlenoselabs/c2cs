// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;
using static my_c_library_namespace.my_c_library;
using static my_c_library_namespace.my_c_library.Runtime;

internal static class Program
{
    private static unsafe void Main()
    {
        Setup();

        hw_hello_world();
        hw_pass_string("Hello world from C#!");
        hw_pass_integers_by_value(65449, -255, 24242);

        ushort a = 65449;
        var b = -255;
        ulong c = 24242;
        hw_pass_integers_by_reference(&a, &b, &c);

        var callback = default(FnPtr_CString_Void);
        callback.Pointer = &Callback;
        hw_invoke_callback(callback, "Hello from callback!");
    }

    [UnmanagedCallersOnly]
    private static void Callback(CString param)
    {
        // This C# function is called from C

        // Get the cached string and print it
        var str = param.ToString();
        Console.WriteLine(str);
        // Free the cached string
        CStrings.FreeCString(param);
    }
}
