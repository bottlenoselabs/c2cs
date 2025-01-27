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
        // NOTE: Create an `INativeAllocator` for allocating native memory such as CStrings!
        //  If you don't want to use `ArenaNativeAllocator`, you can create your own `INativeAllocator` implementation!
        //  The `ArenaNativeAllocator` with the `using` keyword here means that at the end of this scope the `Dispose` method is called for you freeing the native memory.
        //  Thus purpose of the `ArenaNativeAllocator` is that you can "allocate" (re-use the pre-allocated block of memory) a bunch of times without remembering to free the native memory.
        using var allocator = new ArenaNativeAllocator((int)Math.Pow(1024, 2)); // 1 KB

        hw_hello_world();

#if NET7_0_OR_GREATER
        // NOTE: If you apply the `u8` suffix, it's a UTF-8 string literal and does not allocate the string in native memory!
        //  Only available in C# 11 (.NET 7+). See https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/reference-types#utf-8-string-literals
        var cString1 = "Hello world from C# using UTF-8 string literal! No need to free this string!"u8;
        hw_pass_string(cString1);

        // NOTE: This is particularly useful if you have C defines to strings which are stored in the data segment of the loaded C library.
        hw_pass_string(HW_STRING_POINTER);
#endif

        // NOTE: If you don't apply the `u8` suffix it's a UTF-16 string which needs to be converted to UTF-8 and allocated.
        //  This is done by calling `CString.FromString` or using the IAllocator extensions.
        var cString2 = CString.FromString(allocator, "Hello world from C# using UTF-16 converted UTF-8 and allocated! Don't forgot to free this string!");
        hw_pass_string(cString2);

        // NOTE: Same as above, but using the `IAllocator` extensions.
        var cString3 = allocator.AllocateCString("Hello world again from C# using UTF-16 converted UTF-8 and allocated! Don't forgot to free this string!");
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

        var cStringCallback1 = allocator.AllocateCString("Hello from callback!");
        hw_invoke_callback1(functionPointer1, cStringCallback1);
        allocator.Free(cStringCallback1);

        var cStringCallback2 = allocator.AllocateCString("Hello again from callback!");
        hw_invoke_callback2(functionPointer2, cStringCallback2);
        allocator.Free(cStringCallback2);

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
        event1.string1 = allocator.AllocateCString("Anonymous structs and unions have their fields inlined in C# with using the same value for the FieldOffset attribute.");
        event1.string2 = allocator.AllocateCString("If the struct is larger than 16-24 bytes (as is the case here), consider passing it by reference rather than by value.");
        hw_pass_struct_by_value(event1);

        var event2 = default(hw_event);
        event2.kind = hw_event_kind.HW_EVENT_KIND_BOOL;
        event2.boolean = true;
        hw_pass_struct_by_reference(&event2);
    }

#if NET5_0_OR_GREATER
    // NOTE: Function pointers need to use the UnmanagedCallersOnly attribute. See https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.unmanagedcallersonlyattribute?view=net-9.0
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
#endif
    private static void Callback(CString param)
    {
        // This C# function is called from C
        // Get the string and print it
        var str = CString.ToString(param);
        Console.WriteLine(str);
    }
}
