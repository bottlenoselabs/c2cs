// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;

namespace C2CS.WriteCodeCSharp.Data;

public sealed class CSharpFunctionParameter : CSharpNode
{
    public readonly string TypeName;

    public CSharpFunctionParameter(
        string name,
        string className,
        string cName,
        int? sizeOf,
        string typeName)
        : base(name, className, cName, sizeOf)
    {
        TypeName = typeName;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpFunctionParameter other2)
        {
            return false;
        }

        var result = TypeName == other2.TypeName;
        return result;
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var hashCode = HashCode.Combine(baseHashCode, TypeName);
        return hashCode;
    }
}
