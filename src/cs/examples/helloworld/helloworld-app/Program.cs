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

#if NET7_0_OR_GREATER
        // NOTE: If you apply the `u8`, it's a UTF-8 string literal and does not allocate the string on the heap!
        //  Only available in C# 11 (.NET 7+). See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/reference-types#utf-8-string-literals
        var cString1 = (CString)"Hello world from C# using UTF-8 string literal! No need to free this string!"u8;
        hw_pass_string(cString1);

        // NOTE: This is particularly useful if you have C defines to strings which are stored in the data segment of the loaded C library.
        hw_pass_string(HW_STRING_POINTER);
#endif

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

        hw_pass_integers_by_value(65449, 255, 24242);

        ushort a = 65449;
        var b = 255U;
        ulong c = 24242;
        hw_pass_integers_by_reference(&a, &b, &c);

#if NET5_0_OR_GREATER
        // NOTE: Function pointers provide a more efficient way to execute callbacks from C instead of using delegates.
        //  A struct will be generated to "house" the function pointer regardless, in this case: `FnPtr_CString_Void`.
        //      - It uses the same naming as `System.Func<>`. The last type on the name is always the return type. In this case 'void`.
        //  Only available in C# 9 (.NET 5+). See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code#function-pointers
        //  Additionally function pointers need to use the `address-of` operator (&) to a C# static function marked with the UnmanagedCallersOnly attribute. See https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.unmanagedcallersonlyattribute?view=net-9.0
        var functionPointer1 = new hw_callback(&Callback);
        var functionPointer2 = new FnPtr_CString_Void(&Callback);
#else
        var functionPointer1 = new hw_callback(Callback);
        var functionPointer2 = new FnPtr_CString_Void(Callback);
#endif

        using var cStringCallback1 = (CString)"Hello from callback!";
        hw_invoke_callback1(functionPointer1, cStringCallback1);

        using var cStringCallback2 = (CString)"Hello again from callback!";
        hw_invoke_callback2(functionPointer2, cStringCallback2);

        var weekday = DateTime.UtcNow.DayOfWeek switch
        {
            DayOfWeek.Monday => hw_week_day.HW_WEEK_DAY_MONDAY,
            DayOfWeek.Tuesday => hw_week_day.HW_WEEK_DAY_TUESDAY,
            DayOfWeek.Wednesday => hw_week_day.HW_WEEK_DAY_WEDNESDAY,
            DayOfWeek.Thursday => hw_week_day.HW_WEEK_DAY_THURSDAY,
            DayOfWeek.Friday => hw_week_day.HW_WEEK_DAY_FRIDAY,
            DayOfWeek.Sunday => hw_week_day.HW_WEEK_DAY_UNKNOWN,
            DayOfWeek.Saturday => hw_week_day.HW_WEEK_DAY_UNKNOWN,
            _ => hw_week_day.HW_WEEK_DAY_UNKNOWN
        };

        hw_pass_enum_by_value(weekday);
        hw_pass_enum_by_reference(&weekday);

        var event1 = default(hw_event);
        event1.kind = hw_event_kind.HW_EVENT_KIND_STRING;
        event1.string1 = "Anonymous structs and unions have their fields inlined in C# with using the same value for the FieldOffset attribute.";
        event1.string2 = "If the struct is larger than 16-24 bytes (as is the case here), consider passing it by reference rather than by value.";
        hw_pass_struct_by_value(event1);

        var event2 = default(hw_event);
        event2.kind = hw_event_kind.HW_EVENT_KIND_BOOL;
        event2.boolean = true;
        hw_pass_struct_by_reference(&event2);
    }

#if NET5_0_OR_GREATER
    // NOTE: Function pointers need to use the UnmanagedCallersOnly attribute. See https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.unmanagedcallersonlyattribute?view=net-9.0
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
