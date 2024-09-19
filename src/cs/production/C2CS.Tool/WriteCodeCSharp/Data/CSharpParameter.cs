// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.WriteCodeCSharp.Data;

public sealed class CSharpParameter : CSharpNode
{
    public readonly CSharpType Type;

    public CSharpParameter(
        string name,
        string className,
        string cName,
        int? sizeOf,
        CSharpType type)
        : base(name, className, cName, sizeOf)
    {
        Type = type;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpParameter other2)
        {
            return false;
        }

        return Type == other2.Type;
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var hashCode = HashCode.Combine(baseHashCode, Type);
        return hashCode;
    }
}
