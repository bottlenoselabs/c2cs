// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using C2CS.Contexts.ReadCodeC.Data.Serialization;

namespace C2CS.Contexts.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public class CTypeInfo
{
#pragma warning disable CA2211
    public static readonly CTypeInfo Void = new()
    {
        Name = "void",
        Kind = CKind.Primitive,
        SizeOf = 0,
        AlignOf = null,
        ArraySizeOf = null,
        Location = CLocation.NoLocation,
        IsAnonymous = null
    };

    public static CTypeInfo VoidPointer(int pointerSize)
    {
        return new CTypeInfo
        {
            Name = "void*",
            Kind = CKind.Pointer,
            SizeOf = pointerSize,
            AlignOf = pointerSize,
            ElementSize = null,
            ArraySizeOf = null,
            Location = CLocation.NoLocation,
            IsAnonymous = null,
            InnerTypeInfo = Void
        };
    }
#pragma warning restore CA2211

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public CKind Kind { get; set; } = CKind.Unknown;

    [JsonPropertyName("size_of")]
    public int SizeOf { get; set; }

    [JsonPropertyName("align_of")]
    public int? AlignOf { get; set; }

    [JsonPropertyName("size_of_element")]
    public int? ElementSize { get; set; }

    [JsonPropertyName("array_size")]
    public int? ArraySizeOf { get; set; }

    [JsonPropertyName("is_anonymous")]
    public bool? IsAnonymous { get; set; }

    [JsonPropertyName("location")]
    [JsonConverter(typeof(CLocationJsonConverter))]
    public CLocation Location { get; set; }

    [JsonPropertyName("inner_type")]
    public CTypeInfo? InnerTypeInfo { get; set; }

    [ExcludeFromCodeCoverage]
    public override string ToString()
    {
        return Name;
    }

// #pragma warning disable CA1815
// #pragma warning disable CA1034
// #pragma warning disable SA1307
//     // ReSharper disable InconsistentNaming
//     [StructLayout(LayoutKind.Explicit, Size = 32, Pack = 8)]
//     public unsafe struct CXCursor
//     {
//         [FieldOffset(0)] // size = 4, padding = 0
//         public readonly clang.CXCursorKind kind;
//
//         [FieldOffset(4)] // size = 4, padding = 0
//         public readonly int xdata;
//
//         [FieldOffset(8)] // size = 24, padding = 0
//         public fixed byte _data[24]; // void*[3]
//     }
//
//     [StructLayout(LayoutKind.Explicit, Size = 24, Pack = 8)]
//     public unsafe struct CXType
//     {
//         [FieldOffset(0)] // size = 4, padding = 4
//
//         public clang.CXTypeKind kind;
//
//         [FieldOffset(8)] // size = 16, padding = 0
//         public fixed byte _data[16]; // void*[2]
//     }
}
