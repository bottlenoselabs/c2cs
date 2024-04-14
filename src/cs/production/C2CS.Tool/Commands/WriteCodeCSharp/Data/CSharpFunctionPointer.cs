// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using C2CS.Foundation;

namespace C2CS.Commands.WriteCodeCSharp.Data;

public sealed class CSharpFunctionPointer : CSharpNode
{
    public readonly ImmutableArray<CSharpParameter> Parameters;

    public readonly CSharpType ReturnType;

    public CSharpFunctionPointer(
        string name,
        string className,
        string cName,
        int? sizeOf,
        CSharpType returnType,
        ImmutableArray<CSharpParameter> parameters)
        : base(name, className, cName, sizeOf)
    {
        Parameters = parameters;
        ReturnType = returnType;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpFunctionPointer other2)
        {
            return false;
        }

        return ReturnType == other2.ReturnType &&
               Parameters.SequenceEqual(other2.Parameters);
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var parametersHashCode = Parameters.GetHashCodeMembers();
        var hashCode = HashCode.Combine(baseHashCode, ReturnType, parametersHashCode);
        return hashCode;
    }
}
