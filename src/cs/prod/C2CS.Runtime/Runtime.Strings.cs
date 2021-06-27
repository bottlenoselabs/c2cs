// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

#nullable enable

namespace C2CS
{
    public static unsafe partial class Runtime
    {
        private static readonly Dictionary<uint, CString> StringHashesToPointers = new();

        private static readonly Dictionary<IntPtr, string>
            PointersToStrings = new(); // use `IntPtr` as the key for better performance

        // NOTE: On portability, technically `char` in C could be signed or unsigned depending on the computer architecture,
        //  resulting in technically two different type bindings when transpiling C headers to C#. However, to make peace
        //  with the world, I settle on a compromise:
        //      `CString` is `char*`. When exposing public functions of ANSI/UTF8 strings in C#, you should only care about
        //      `char*` as a single "thing" not about it's parts "char" and "*".

        /// <summary>
        ///     Converts a <see cref="string" /> from a C style string (one dimensional byte array terminated by a
        ///     <c>0x0</c>) by allocating and copying.
        /// </summary>
        /// <param name="ptr">A pointer to the C string.</param>
        /// <returns>A <see cref="string" /> equivalent of <paramref name="ptr" />.</returns>
        public static string String(CString ptr)
        {
            if (ptr.IsNull)
            {
                return string.Empty;
            }

            if (PointersToStrings.TryGetValue(ptr, out var result))
            {
                return result;
            }

            var hash = djb2((byte*) ptr._value);
            if (StringHashesToPointers.TryGetValue(hash, out var pointer2))
            {
                result = PointersToStrings[pointer2];
                return result;
            }

#if NETCOREAPP
            // calls ASM/C/C++ functions to calculate length and then "FastAllocate" the string with the GC
            // https://mattwarren.org/2016/05/31/Strings-and-the-CLR-a-Special-Relationship/
            result = Marshal.PtrToStringAnsi(ptr);
#else
        // if you get here you need to ask yourself:
        //  (1) how do I calculate the length of a ANSI string?
        //  (2) how do I convert the ANSI string to a UTF-16 string? Note that each `char` in C# is UTF-16, 2 bytes.
        //  (3) how and where do I store the memory for the resulting UTF-16 string?
        throw new NotImplementedException();
#endif

            if (string.IsNullOrEmpty(result))
            {
                return string.Empty;
            }

            StringHashesToPointers.Add(hash, ptr);
            PointersToStrings.Add(ptr, result);

            return result;
        }

        /// <summary>
        ///     Converts a C string pointer (one dimensional byte array terminated by a
        ///     <c>0x0</c>) for a specified <see cref="string" /> by allocating and copying.
        /// </summary>
        /// <param name="str">The <see cref="string" />.</param>
        /// <returns>A C string pointer.</returns>
        public static CString CString(string str)
        {
            var hash = djb2(str);
            if (StringHashesToPointers.TryGetValue(hash, out var r))
            {
                return r;
            }

            // ReSharper disable once JoinDeclarationAndInitializer
            IntPtr pointer;
#if NETCOREAPP
            pointer = Marshal.StringToHGlobalAnsi(str);
#else
        // if you get here you need to ask yourself:
        //  (1) how do I calculate the number of bytes required for the ANSI string from an UTP-16 C# string?
        //  (2) how do I convert the UTF-16 string to an ANSI string? Note that each `char` in C# is UTF-16, 2 bytes.
        //  (3) how and where do I store the memory for the resulting ANSI string?
        throw new NotImplementedException();
#endif
            StringHashesToPointers.Add(hash, new CString(pointer));
            PointersToStrings.Add(pointer, str);

            return new CString(pointer);
        }

        /// <summary>
        ///     Converts an array of strings to an array of C strings (multi-dimensional array of one
        ///     dimensional byte arrays each terminated by a <c>0x0</c>) by allocating and copying.
        /// </summary>
        /// <remarks>
        ///     <para>Calls <see cref="CString" />.</para>
        /// </remarks>
        /// <param name="values">The strings.</param>
        /// <returns>An array pointer of C string pointers. You are responsible for freeing the returned pointer.</returns>
        public static CString* CStringArray(ReadOnlySpan<string> values)
        {
            var pointerSize = IntPtr.Size;
            var result = (CString*) AllocateMemory(pointerSize * values.Length);
            for (var i = 0; i < values.Length; ++i)
            {
                var @string = values[i];
                var cString = CString(@string);
                result[i] = cString;
            }

            return result;
        }

        /// <summary>
        ///     Frees the memory for all previously allocated C strings and releases references to all <see cref="string" />
        ///     objects which happened during <see cref="String" /> or <see cref="CString" />. Does
        ///     <b>not</b> garbage collect.
        /// </summary>
        public static void FreeAllStrings()
        {
            foreach (var (ptr, _) in PointersToStrings)
            {
                Marshal.FreeHGlobal(ptr);
            }

            // We can not guarantee that the application has not a strong reference the string since it was allocated,
            //  so we have to let the GC take the wheel here. Thus, this method should NOT garbage collect; that's
            //  on the responsibility of the application developer. The best we can do is just remove any and all strong
            //  references we have here to the strings.

            StringHashesToPointers.Clear();
            PointersToStrings.Clear();
        }

        /// <summary>
        ///     Frees the memory for specific previously allocated C strings and releases associated references to
        ///     <see cref="string" /> objects which happened during <see cref="String" /> or
        ///     <see cref="CString" />. Does <b>not</b> garbage collect.
        /// </summary>
        /// <param name="ptrs">The C string pointers.</param>
        /// <param name="count">The number of C string pointers.</param>
        public static void FreeCStrings(CString* ptrs, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var ptr = ptrs[i];
                FreeCString(ptr);
            }

            FreeMemory(ptrs);
        }

        /// <summary>
        ///     Frees the memory for the previously C strings and releases reference to the <see cref="string" /> object
        ///     which happened during <see cref="String" />, <see cref="CString" />. Does <b>not</b> garbage
        ///     collect.
        /// </summary>
        /// <param name="ptr">The string.</param>
        public static void FreeCString(CString ptr)
        {
            if (!PointersToStrings.ContainsKey(ptr._value))
            {
                return;
            }

            Marshal.FreeHGlobal(ptr);
            var hash = djb2(ptr);
            StringHashesToPointers.Remove(hash);
            PointersToStrings.Remove(ptr._value);
        }

        // From the mind of: https://en.wikipedia.org/wiki/Daniel_J._Bernstein
        //  References:
        //  (1) https://stackoverflow.com/a/7666577/2171957
        //  (2) http://www.cse.yorku.ca/~oz/hash.html
        //  (3) https://groups.google.com/g/comp.lang.c/c/lSKWXiuNOAk/m/zstZ3SRhCjgJ
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Algorithm name.")]
        internal static uint djb2(byte* str)
        {
            // hash(i) = hash(i - 1) * 33 ^ str[i]

            uint hash = 5381;
            uint c;

            while ((c = *str++) != 0)
            {
                hash = (hash << 5) + hash + c; /* hash * 33 + c */
            }

            return hash;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Algorithm name.")]
        internal static uint djb2(string str)
        {
            // hash(i) = hash(i - 1) * 33 ^ str[i]

            uint hash = 5381;

            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var c in str)
            {
                hash = (hash << 5) + hash + c; /* hash * 33 + c */
            }

            return hash;
        }
    }
}
