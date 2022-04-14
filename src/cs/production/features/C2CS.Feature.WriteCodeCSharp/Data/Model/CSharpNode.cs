// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.WriteCodeCSharp.Data.Model;

public abstract class CSharpNode : IEquatable<CSharpNode>
{
    public readonly TargetPlatform Platform;

    public readonly string Name;

    public readonly string CodeLocationComment;

    public readonly int? SizeOf;

    protected CSharpNode(TargetPlatform platform, string? name, string? codeLocationComment, int? sizeOf)
    {
        Platform = platform;
        Name = string.IsNullOrEmpty(name) ? string.Empty : name;
        CodeLocationComment = string.IsNullOrEmpty(codeLocationComment) ? string.Empty : codeLocationComment;
        SizeOf = sizeOf;
    }

    public override string ToString()
    {
        return $"{Name} {CodeLocationComment} {Platform}";
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
        if (!result)
        {
            Console.WriteLine();
        }

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

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, CodeLocationComment, SizeOf);
    }
}
