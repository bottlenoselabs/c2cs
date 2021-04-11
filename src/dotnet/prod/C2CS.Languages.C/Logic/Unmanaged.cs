// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace C2CS.Languages.C
{
    public static unsafe class Unmanaged
    {
        private static readonly Dictionary<uint, IntPtr> StringHashesToPointers = new();
        private static readonly Dictionary<IntPtr, string> PointersToStrings = new();

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

            return result;
        }

        public static sbyte* MapCString(string @string)
        {
            var hash = Crc32B(@string);
            if (StringHashesToPointers.TryGetValue(hash, out var pointer))
            {
                return (sbyte*)pointer;
            }

            pointer = Marshal.StringToHGlobalAnsi(@string);
            StringHashesToPointers.Add(hash, pointer);
            PointersToStrings.Add(pointer, @string);

            return (sbyte*)pointer;
        }

        public static void** MapCStringArray(ReadOnlySpan<string> values)
        {
            var pointerSize = IntPtr.Size;
            var result = (void**)Marshal.AllocHGlobal(pointerSize * values.Length);
            for (var i = 0; i < values.Length; ++i)
            {
                var @string = values[i];
                var cString = MapCString(@string);
                result[i] = cString;
            }

            return result;
        }

        public static void** MapCStringArray(ImmutableArray<string> values)
        {
            var result = MapCStringArray(values.AsSpan());
            return result;
        }

        // https://stackoverflow.com/questions/21001659/crc32-algorithm-implementation-in-c-without-a-look-up-table-and-with-a-public-li
        private static uint Crc32B(sbyte *message)
        {
            var i = 0;
            var crc = 0xFFFFFFFF;
            while (message[i] != 0)
            {
                var @byte = (uint) message[i];
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

        private static uint Crc32B(string message)
        {
            var crc = 0xFFFFFFFF;
            foreach (var value in message)
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
}
