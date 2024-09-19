// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.WriteCodeCSharp.Data;

public sealed class CSharpAliasType : CSharpNode
{
    public readonly CSharpType UnderlyingType;

    public CSharpAliasType(
        string name,
        string className,
        string cName,
        int? sizeOf,
        CSharpType underlyingType)
        : base(name, className, cName, sizeOf)
    {
        UnderlyingType = underlyingType;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpAliasType other2)
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
