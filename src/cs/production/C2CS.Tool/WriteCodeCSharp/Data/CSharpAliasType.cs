// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;

namespace C2CS.WriteCodeCSharp.Data;

public sealed class CSharpAliasType : CSharpNode
{
    public readonly CSharpTypeInfo UnderlyingTypeInfo;

    public CSharpAliasType(
        string name,
        int? sizeOf,
        CSharpTypeInfo underlyingTypeInfo,
        ImmutableArray<Attribute> attributes)
        : base(name, sizeOf, attributes)
    {
        UnderlyingTypeInfo = underlyingTypeInfo;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpAliasType other2)
        {
            return false;
        }

        return UnderlyingTypeInfo == other2.UnderlyingTypeInfo;
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var hashCode = HashCode.Combine(baseHashCode, UnderlyingTypeInfo);
        return hashCode;
    }
}
