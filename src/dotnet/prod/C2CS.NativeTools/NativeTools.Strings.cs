// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#nullable enable

public static unsafe partial class NativeTools
{
    private static readonly Dictionary<uint, IntPtr> StringHashesToPointers = new();
    private static readonly Dictionary<IntPtr, string> PointersToStrings = new();
    private static readonly List<IntPtr> Pointers = new();

    /// <summary>
    ///     Gets a <see cref="string" /> from a C style string (one dimensional <see cref="sbyte" /> array
    ///     terminated by a <c>0x0</c>).
    /// </summary>
    /// <param name="cString">A pointer to the C string.</param>
    /// <returns>A <see cref="string" /> equivalent to the C string pointed by <paramref name="cString" />.</returns>
    public static string MapString(sbyte* cString)
    {
        var pointer = (IntPtr) cString;
        if (PointersToStrings.TryGetValue(pointer, out var result))
        {
            return result;
        }

        var hash = Crc32B(cString);
        if (StringHashesToPointers.TryGetValue(hash, out var pointer2))
        {
            result = PointersToStrings[pointer2];
            return result;
        }

        result = Marshal.PtrToStringAnsi(pointer);
        if (string.IsNullOrEmpty(result))
        {
            return string.Empty;
        }

        StringHashesToPointers.Add(hash, pointer);
        PointersToStrings.Add(pointer, result);
        Pointers.Add(pointer);

        return result;
    }

    /// <summary>
    ///     Gets a C string pointer (a one dimensional <see cref="sbyte" /> array terminated by a <c>0x0</c>) from a
    ///     <see cref="string" />.
    /// </summary>
    /// <param name="string">A <see cref="string" />.</param>
    /// <returns>A C string pointer.</returns>
    public static sbyte* MapCString(string @string)
    {
        var hash = Crc32B(@string);
        if (StringHashesToPointers.TryGetValue(hash, out var pointer))
        {
            return (sbyte*) pointer;
        }

        pointer = Marshal.StringToHGlobalAnsi(@string);
        StringHashesToPointers.Add(hash, pointer);
        PointersToStrings.Add(pointer, @string);
        Pointers.Add(pointer);

        return (sbyte*) pointer;
    }

    /// <summary>
    ///     Gets an array pointer of C string pointers (pointer to multiple one dimensional <see cref="sbyte" />
    ///     arrays, each of which is terminated by a <c>0x0</c>) from a <see cref="ReadOnlySpan{string}" />.
    /// </summary>
    /// <param name="values">The strings.</param>
    /// <returns>An array pointer of C string pointers.</returns>
    public static void** MapCStringArray(ReadOnlySpan<string> values)
    {
        var pointerSize = IntPtr.Size;
        var bytes = (byte*) Marshal.AllocHGlobal(pointerSize * values.Length);
        Pointers.Add((IntPtr) bytes);
        var result = (void**) bytes;
        for (var i = 0; i < values.Length; ++i)
        {
            var @string = values[i];
            var cString = MapCString(@string);
            result[i] = cString;
        }

        return result;
    }

    /// <summary>
    ///     Frees the memory for all allocated C strings and releases references to all <see cref="string" />
    ///     objects which happened during <see cref="MapString" />, <see cref="MapCString" />,
    ///     or <see cref="MapCStringArray" />. Does <b>not</b> garbage collect.
    /// </summary>
    public static void ClearStrings()
    {
        foreach (var pointer in Pointers)
        {
            Marshal.FreeHGlobal(pointer);
        }

        StringHashesToPointers.Clear();
        PointersToStrings.Clear();
        Pointers.Clear();
    }

    // https://stackoverflow.com/questions/21001659/crc32-algorithm-implementation-in-c-without-a-look-up-table-and-with-a-public-li
    private static uint Crc32B(sbyte* cString)
    {
        var i = 0;
        var crc = 0xFFFFFFFF;
        while (cString[i] != 0)
        {
            var @byte = (uint) cString[i];
            crc ^= @byte;
            int j;
            for (j = 7; j >= 0; j--)
            {
                // Do eight times.
                var mask = (uint) -(crc & 1);
                crc = (crc >> 1) ^ (0xEDB88320 & mask);
            }

            i += 1;
        }

        return ~crc;
    }

    private static uint Crc32B(string @string)
    {
        var crc = 0xFFFFFFFF;
        foreach (var value in @string)
        {
            var @byte = (uint) value;
            crc ^= @byte;
            int j;
            for (j = 7; j >= 0; j--)
            {
                // Do eight times.
                var mask = (uint) -(crc & 1);
                crc = (crc >> 1) ^ (0xEDB88320 & mask);
            }
        }

        return ~crc;
    }
}
