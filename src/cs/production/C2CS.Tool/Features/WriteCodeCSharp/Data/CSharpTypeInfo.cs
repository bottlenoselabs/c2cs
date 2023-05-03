// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;

namespace C2CS.Features.WriteCodeCSharp.Data;

public sealed class CSharpTypeInfo : IEquatable<CSharpTypeInfo>
{
    public string Name { get; init; } = string.Empty;

    public string? OriginalName { get; init; }

    public int SizeOf { get; init; }

    public int? AlignOf { get; init; }

    public int? ArraySizeOf { get; init; }

    public bool IsArray => ArraySizeOf > 0;

    public ImmutableArray<Attribute> Attributes { get; init; }

    public bool Equals(CSharpTypeInfo? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        var isEqual = Name == other.Name &&
                      SizeOf == other.SizeOf &&
                      AlignOf == other.AlignOf &&
                      ArraySizeOf == other.ArraySizeOf;
        return isEqual;
    }

    public override string ToString()
    {
        return string.IsNullOrEmpty(Name) ? string.Empty : Name;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CSharpTypeInfo);
    }

    public static bool operator ==(CSharpTypeInfo? a, CSharpTypeInfo? b)
    {
        if (a is null)
        {
            if (b is null)
            {
                return true;
            }

            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(CSharpTypeInfo? a, CSharpTypeInfo? b) => !(a == b);

    public override int GetHashCode()
    {
        var hashCode = HashCode.Combine(Name, SizeOf, AlignOf, ArraySizeOf);
        return hashCode;
    }
}
