// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using static my_c_library_namespace.my_c_library;
using static my_c_library_namespace.my_c_library.Runtime;

internal static class Program
{
    private static unsafe void Main()
    {
        Setup();

        hello_world();
        pass_string("Hello world from C#!");
        pass_integers_by_value(65449, -255, 24242);

        ushort a = 65449;
        var b = -255;
        ulong c = 24242;
        pass_integers_by_reference(&a, &b, &c);

        // Allocate a delegate on the heap and marshal a pointer to it so we can trampoline back to C#
        var del = new FnPtr_CString_Void(Callback);
        // String here is cached
        invoke_callback(del, "Hello from callback!");
    }

    private static void Callback(CString param)
    {
        // Get the cached string
        var str = param.ToString();

        Console.WriteLine(str);

        // Free the cached string
        CStrings.FreeCString(param);
    }
}
