// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Data.CSharp.Model;

public abstract class CSharpNode : IEquatable<CSharpNode>
{
    public readonly string CodeLocationComment;

    public readonly string Name;
    public readonly ImmutableArray<TargetPlatform> Platforms;

    public readonly int? SizeOf;

    protected CSharpNode(
        ImmutableArray<TargetPlatform> platforms,
        string? name,
        string? codeLocationComment,
        int? sizeOf)
    {
        Platforms = platforms;
        Name = string.IsNullOrEmpty(name) ? string.Empty : name;
        CodeLocationComment = string.IsNullOrEmpty(codeLocationComment) ? string.Empty : codeLocationComment;
        SizeOf = sizeOf;
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

    public override string ToString()
    {
        return $"{Name} {CodeLocationComment} {Platforms}";
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
            if (b is null)
            {
                return true;
            }

            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(CSharpNode? a, CSharpNode? b) => !(a == b);

    public override int GetHashCode()
    {
        var hashCode = HashCode.Combine(Name, SizeOf);
        return hashCode;
    }
}
