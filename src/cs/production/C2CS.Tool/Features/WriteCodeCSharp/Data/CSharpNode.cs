// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;

namespace C2CS.Features.WriteCodeCSharp.Data;

public abstract class CSharpNode : IEquatable<CSharpNode>, IComparable<CSharpNode>
{
    public readonly string Name;
    public readonly int? SizeOf;
    public readonly ImmutableArray<Attribute> Attributes;

    protected CSharpNode(
        string? name,
        int? sizeOf,
        ImmutableArray<Attribute> attributes)
    {
        Name = string.IsNullOrEmpty(name) ? string.Empty : name;
        SizeOf = sizeOf;
        Attributes = attributes;
    }

    public virtual bool Equals(CSharpNode? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        var result = Name == other.Name && SizeOf == other.SizeOf;
        return result;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj.GetType() == GetType() && Equals((CSharpNode)obj);
    }

    public static bool operator ==(CSharpNode? a, CSharpNode? b)
    {
        if (a is null)
        {
            return b is null;
        }

        return a.Equals(b);
    }

    public static bool operator !=(CSharpNode? a, CSharpNode? b) => !(a == b);

    public override int GetHashCode()
    {
        var hashCode = HashCode.Combine(Name, SizeOf);
        return hashCode;
    }

    public override string ToString()
    {
        return $"{Name}";
    }

    public int CompareTo(CSharpNode? other)
    {
        if (ReferenceEquals(this, other))
        {
            return 0;
        }

        if (ReferenceEquals(null, other))
        {
            return 1;
        }

        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0)
        {
            return nameComparison;
        }

        return 0;
    }

    public static bool operator <(CSharpNode left, CSharpNode right)
    {
        return ReferenceEquals(left, null) ? !ReferenceEquals(right, null) : left.CompareTo(right) < 0;
    }

    public static bool operator <=(CSharpNode left, CSharpNode right)
    {
        return ReferenceEquals(left, null) || left.CompareTo(right) <= 0;
    }

    public static bool operator >(CSharpNode left, CSharpNode right)
    {
        return !ReferenceEquals(left, null) && left.CompareTo(right) > 0;
    }

    public static bool operator >=(CSharpNode left, CSharpNode right)
    {
        return ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.CompareTo(right) >= 0;
    }
}
