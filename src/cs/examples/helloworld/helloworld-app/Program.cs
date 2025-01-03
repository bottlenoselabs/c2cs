// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Interop.Runtime;
using static helloworld.my_c_library;

internal static class Program
{
    private static unsafe void Main()
    {
        hw_hello_world();

        // NOTE: If you apply the `u8`, it's a UTF-8 string literal and does not allocate the string on the heap!
        //  See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/reference-types#utf-8-string-literals
        var cString1 = (CString)"Hello world from C# using UTF-8 string literal! No need to free this string!"u8;
        hw_pass_string(cString1);

        // NOTE: If you don't apply the `u8` it's a UTF-16 string which needs to be converted to UTF-8 and allocated.
        //  This is done by calling `CString.FromString` or using the explicit CString conversion operator.
        //  You additionally need to call `Marshal.FreeHGlobal()` when you are done with it or you have a memory leak!
        var cString2 = CString.FromString("Hello world from C# using UTF-16 converted UTF-8 and allocated! Don't forgot to free this string!");
        hw_pass_string(cString2);
        Marshal.FreeHGlobal(cString2);

        // NOTE: You can also use `using` syntax so you don't forgot to call `Marshal.FreeHGlobal()` at the scope end.
        //  Just don't use `using` syntax when using UTF-8 string literals or your app will crash!
        using var cString3 = (CString)"Hello world again from C# using UTF-16 converted UTF-8 and allocated! Don't forgot to free this string!";
        hw_pass_string(cString3);

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
        using var cStringCallback = (CString)"Hello from callback!";
        hw_invoke_callback(functionPointer, cStringCallback);
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
