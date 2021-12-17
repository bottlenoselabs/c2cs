using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

/// <summary>
///     The collection of utility methods for interoperability with C style strings in C#.
/// </summary>
public static unsafe class CStrings
{
    private static readonly Dictionary<uint, CString> StringHashesToPointers = new();
    private static readonly Dictionary<nint, string> PointersToStrings = new();
    private static readonly Dictionary<uint, CStringWide> StringHashesToPointersWide = new();
    private static readonly Dictionary<nint, string> PointersToStringsWide = new();

    // NOTE: On portability, technically `char` in C could be signed or unsigned depending on the computer architecture,
    //  resulting in technically two different type bindings when transpiling C headers to C#. However, to make peace
    //  with the world, I settle on a compromise:
    //      `CString` is `char*`. When exposing public functions of ANSI/UTF8 strings in C#, you should only care about
    //      `char*` as a single "thing" not about it's parts "char" and "*".

    /// <summary>
    ///     Converts a C style string (ANSI or UTF-8) of type `char` (one dimensional byte array
    ///     terminated by a <c>0x0</c>) to a UTF-16 <see cref="string" /> by allocating and copying if not already cached.
    /// </summary>
    /// <param name="value">A pointer to the C string.</param>
    /// <returns>A <see cref="string" /> equivalent of <paramref name="value" />.</returns>
    public static string String(CString value)
    {
        if (value.IsNull)
        {
            return string.Empty;
        }

        if (PointersToStrings.TryGetValue(value._pointer, out var result))
        {
            return result;
        }

        var hash = Djb2((byte*)value._pointer);
        if (StringHashesToPointers.TryGetValue(hash, out var pointer2))
        {
            result = PointersToStrings[pointer2._pointer];
            return result;
        }

        // calls ASM/C/C++ functions to calculate length and then "FastAllocate" the string with the GC
        // https://mattwarren.org/2016/05/31/Strings-and-the-CLR-a-Special-Relationship/
        result = Marshal.PtrToStringAnsi(value._pointer);

        if (string.IsNullOrEmpty(result))
        {
            return string.Empty;
        }

        StringHashesToPointers.Add(hash, value);
        PointersToStrings.Add(value._pointer, result);

        return result;
    }

    /// <summary>
    ///     Converts a C style string (unicode) of type `wchar_t` (one dimensional ushort array
    ///     terminated by a <c>0x0</c>) to a UTF-16 <see cref="string" /> by allocating and copying if not already cached.
    /// </summary>
    /// <param name="value">A pointer to the C string.</param>
    /// <returns>A <see cref="string" /> equivalent of <paramref name="value" />.</returns>
    public static string StringWide(CStringWide value)
    {
        if (value.IsNull)
        {
            return string.Empty;
        }

        if (PointersToStringsWide.TryGetValue(value._pointer, out var result))
        {
            return result;
        }

        var hash = Djb2((byte*)value._pointer);
        if (StringHashesToPointersWide.TryGetValue(hash, out var pointer2))
        {
            result = PointersToStringsWide[pointer2._pointer];
            return result;
        }

        // calls ASM/C/C++ functions to calculate length and then "FastAllocate" the string with the GC
        // https://mattwarren.org/2016/05/31/Strings-and-the-CLR-a-Special-Relationship/
        result = Marshal.PtrToStringUni(pointer2._pointer);

        if (string.IsNullOrEmpty(result))
        {
            return string.Empty;
        }

        StringHashesToPointersWide.Add(hash, value);
        PointersToStringsWide.Add(value._pointer, result);

        return result;
    }

    /// <summary>
    ///     Converts a UTF-16 <see cref="string" /> to a C style string (one dimensional byte array terminated by a
    ///     <c>0x0</c>) by allocating and copying if not already cached.
    /// </summary>
    /// <param name="str">The <see cref="string" />.</param>
    /// <returns>A C string pointer.</returns>
    public static CString CString(string str)
    {
        var hash = Djb2(str);
        if (StringHashesToPointers.TryGetValue(hash, out var r))
        {
            return r;
        }

        // ReSharper disable once JoinDeclarationAndInitializer
        var pointer = Marshal.StringToHGlobalAnsi(str);
        StringHashesToPointers.Add(hash, new CString(pointer));
        PointersToStrings.Add(pointer, str);

        return new CString(pointer);
    }

    /// <summary>
    ///     Converts a C string pointer (one dimensional byte array terminated by a
    ///     <c>0x0</c>) for a specified <see cref="string" /> by allocating and copying if not already cached.
    /// </summary>
    /// <param name="str">The <see cref="string" />.</param>
    /// <returns>A C string pointer.</returns>
    public static CStringWide CStringWide(string str)
    {
        var hash = Djb2(str);
        if (StringHashesToPointersWide.TryGetValue(hash, out var r))
        {
            return r;
        }

        // ReSharper disable once JoinDeclarationAndInitializer
        var pointer = Marshal.StringToHGlobalUni(str);
        StringHashesToPointersWide.Add(hash, new CStringWide(pointer));
        PointersToStringsWide.Add(pointer, str);

        return new CStringWide(pointer);
    }

    /// <summary>
    ///     Converts an array of strings to an array of C strings of type `char` (multi-dimensional array of one
    ///     dimensional byte arrays each terminated by a <c>0x0</c>) by allocating and copying if not already cached.
    /// </summary>
    /// <remarks>
    ///     <para>Calls <see cref="CString" />.</para>
    /// </remarks>
    /// <param name="values">The strings.</param>
    /// <returns>An array pointer of C string pointers. You are responsible for freeing the returned pointer.</returns>
    public static CString* CStringArray(ReadOnlySpan<string> values)
    {
        var pointerSize = IntPtr.Size;
        var result = (CString*)Marshal.AllocHGlobal(pointerSize * values.Length);
        for (var i = 0; i < values.Length; ++i)
        {
            var @string = values[i];
            var cString = CString(@string);
            result[i] = cString;
        }

        return result;
    }

    /// <summary>
    ///     Converts an array of strings to an array of C strings of type `wchar_t` (multi-dimensional array of one
    ///     dimensional ushort arrays each terminated by a <c>0x0</c>) by allocating and copying if not already cached.
    /// </summary>
    /// <remarks>
    ///     <para>Calls <see cref="CString" />.</para>
    /// </remarks>
    /// <param name="values">The strings.</param>
    /// <returns>An array pointer of C string pointers. You are responsible for freeing the returned pointer.</returns>
    public static CStringWide* CStringWideArray(ReadOnlySpan<string> values)
    {
        var pointerSize = IntPtr.Size;
        var result = (CStringWide*)Marshal.AllocHGlobal(pointerSize * values.Length);
        for (var i = 0; i < values.Length; ++i)
        {
            var @string = values[i];
            var cString = CStringWide(@string);
            result[i] = cString;
        }

        return result;
    }

    /// <summary>
    ///     Frees the memory for all previously allocated C strings and releases references to all <see cref="string" />
    ///     objects which happened during <see cref="String" />, <see cref="StringWide"/>, <see cref="CString"/>
    ///     or <see cref="CStringWide" />. Does <b>not</b> garbage collect.
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
    /// <param name="pointers">The C string pointers.</param>
    /// <param name="count">The number of C string pointers.</param>
    public static void FreeCStrings(CString* pointers, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var ptr = pointers[i];
            FreeCString(ptr);
        }

        Marshal.FreeHGlobal((IntPtr)pointers);
    }

    /// <summary>
    ///     Frees the memory for the previously allocated C string and releases reference to the
    ///     <see cref="string" /> object which happened during <see cref="String" /> or <see cref="CString" />.
    ///     Does <b>not</b> garbage collect.
    /// </summary>
    /// <param name="value">The string.</param>
    public static void FreeCString(CString value)
    {
        if (!PointersToStrings.ContainsKey(value._pointer))
        {
            return;
        }

        Marshal.FreeHGlobal(value);
        var hash = Djb2(value);
        StringHashesToPointers.Remove(hash);
        PointersToStrings.Remove(value._pointer);
    }

    /// <summary>
    ///     Frees the memory for the previously allocated C string and releases reference to the
    ///     <see cref="string" /> object which happened during <see cref="StringWide" /> or <see cref="CStringWide" />.
    ///     Does <b>not</b> garbage collect.
    /// </summary>
    /// <param name="value">The string.</param>
    public static void FreeCStringWide(CStringWide value)
    {
        if (!PointersToStringsWide.ContainsKey(value._pointer))
        {
            return;
        }

        Marshal.FreeHGlobal(value);
        var hash = Djb2(value);
        StringHashesToPointersWide.Remove(hash);
        PointersToStringsWide.Remove(value._pointer);
    }

    // djb2 is named after https://en.wikipedia.org/wiki/Daniel_J._Bernstein
    //  References:
    //  (1) https://stackoverflow.com/a/7666577/2171957
    //  (2) http://www.cse.yorku.ca/~oz/hash.html
    //  (3) https://groups.google.com/g/comp.lang.c/c/lSKWXiuNOAk/m/zstZ3SRhCjgJ
    private static uint Djb2(byte* str)
    {
        // Lucas Girouard-Stranks: my explanation of djb2
        // basic hash algorithm; we want each character in the string to have some bias related to it's position for calculating the hash
        // this is to prevent strings with the same characters but scrambled to not have the same hash values
        // hash = str[0] + g * (str[1] + g * (str[2] + g * (str[3] + g ... * (str[n-2] + g * (str[n-1] + g)))))
        // hash(-1) = x; hash(i) = (hash(i-1) * g) + str[i];
        // note that `i` is an element of the range inclusive from 0 to n-1, where `n` is the length of the string
        // Daniel Bernstein choose `g` to be 33 and hash(-1) to be 5381
        // this coincides with a linear congruential generator for generating pseudo-randomized numbers: https://en.wikipedia.org/wiki/Linear_congruential_generator
        // the basic LCG algorithm is: x(i) = ((a * x(i -1)) + c) + mod m
        // note that integer overflow is equivalent to a modulus operation of the bit width so it's often the case that `m` = 2^32
        // eureka! notice that djb2 then resembles an LCG: x(i) = a * x(i-1) + c
        // for the LCG to generate as random as possible values the value of `a` has some limitations (period length):
        // 1: `m` and `c` are co-prime
        //  this is true when `c` is odd because the only common divisor that could exist between 2^n and `c` is 2
        //  however the fact that condition is not always met is "okay", it still results in a "good enough" LCG
        // 2: `a-1` is divisible by all prime factors of `m`
        //  `a-1` = 32 which is divisible by 2, so of course it's divisible by the only prime factor of 2^n which is 2
        // 3: `a-1` is divisible by 4 if `m` is divisible by 4
        //  this is true, `a-1` being 32 is divisible by 2^2 and so is 2^n where n >= 2
        // note that a good non-cryptographic hash function is NOT intended to generate random numbers
        // instead the intention is that when inputs are nearly identical (one character is different in the string)
        //  that the output is widely different, this is where the first condition NOT being met MIGHT actually be a good thing
        // hopefully someone smarter than me one day will write or talk about the relationship of pseudorandom number
        //  generator (PRNG) and non-cryptographic hash functions for the purposes of data structures like a hashtable

        uint hash = 5381;

        unchecked
        {
            uint c;
            while ((c = *str++) != 0)
            {
                hash = ((hash << 5) + hash) + c; // hash * 33 + c
            }
        }

        return hash;
    }

    private static uint Djb2(string str)
    {
        uint hash = 5381;

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var c in str)
        {
            hash = (hash << 5) + hash + c; // hash * 33 + c
        }

        return hash;
    }
}
