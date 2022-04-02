// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.WriteCodeCSharp.Data.Model;

public abstract class CSharpNode : IEquatable<CSharpNode>
{
    public readonly string Name;

    public readonly string CodeLocationComment;

    public readonly int? SizeOf;

    protected CSharpNode(string? name, string? codeLocationComment, int? sizeOf)
    {
        Name = string.IsNullOrEmpty(name) ? string.Empty : name;
        CodeLocationComment = string.IsNullOrEmpty(codeLocationComment) ? string.Empty : codeLocationComment;
        SizeOf = sizeOf;
    }

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return $"{Name} {CodeLocationComment}";
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

        return Name == other.Name && SizeOf == other.SizeOf;
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

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, CodeLocationComment, SizeOf);
    }
}
