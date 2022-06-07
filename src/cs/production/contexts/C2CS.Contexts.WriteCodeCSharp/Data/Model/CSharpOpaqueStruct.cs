// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Contexts.WriteCodeCSharp.Data.Model;

public sealed class CSharpOpaqueStruct : CSharpNode
{
    public CSharpOpaqueStruct(
        ImmutableArray<TargetPlatform> platforms,
        string name,
        string codeLocationComment)
        : base(platforms, name, codeLocationComment, null)
    {
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpOpaqueStruct)
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var hashCode = HashCode.Combine(baseHashCode);
        return hashCode;
    }
}
