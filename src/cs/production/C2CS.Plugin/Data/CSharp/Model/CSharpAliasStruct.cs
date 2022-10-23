// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Data.CSharp.Model;

public sealed class CSharpAliasStruct : CSharpNode
{
    public readonly CSharpType UnderlyingType;

    public CSharpAliasStruct(
        ImmutableArray<TargetPlatform> platforms,
        string name,
        string cKind,
        string cCodeLocation,
        int? sizeOf,
        CSharpType underlyingType,
        ImmutableArray<Attribute> attributes)
        : base(platforms, name, cKind, cCodeLocation, sizeOf, attributes)
    {
        UnderlyingType = underlyingType;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpAliasStruct other2)
        {
            return false;
        }

        return UnderlyingType == other2.UnderlyingType;
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var hashCode = HashCode.Combine(baseHashCode, UnderlyingType);
        return hashCode;
    }
}
