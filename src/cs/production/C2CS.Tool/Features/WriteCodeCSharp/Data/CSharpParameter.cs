// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;

namespace C2CS.Features.WriteCodeCSharp.Data;

public sealed class CSharpParameter : CSharpNode
{
    public readonly CSharpTypeInfo TypeInfo;

    public CSharpParameter(
        string name,
        string className,
        string cName,
        int? sizeOf,
        CSharpTypeInfo typeInfo,
        ImmutableArray<Attribute> attributes)
        : base(name, className, cName, sizeOf, attributes)
    {
        TypeInfo = typeInfo;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpParameter other2)
        {
            return false;
        }

        return TypeInfo == other2.TypeInfo;
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var hashCode = HashCode.Combine(baseHashCode, TypeInfo);
        return hashCode;
    }
}
