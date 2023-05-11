// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using C2CS.Foundation;

namespace C2CS.Features.WriteCodeCSharp.Data;

public sealed class CSharpFunctionPointer : CSharpNode
{
    public readonly ImmutableArray<CSharpParameter> Parameters;

    public readonly CSharpTypeInfo ReturnTypeInfo;

    public CSharpFunctionPointer(
        string name,
        string className,
        string cName,
        int? sizeOf,
        CSharpTypeInfo returnTypeInfo,
        ImmutableArray<CSharpParameter> parameters,
        ImmutableArray<Attribute> attributes)
        : base(name, className, cName, sizeOf, attributes)
    {
        Parameters = parameters;
        ReturnTypeInfo = returnTypeInfo;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpFunctionPointer other2)
        {
            return false;
        }

        return ReturnTypeInfo == other2.ReturnTypeInfo &&
               Parameters.SequenceEqual(other2.Parameters);
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var parametersHashCode = Parameters.GetHashCodeMembers();
        var hashCode = HashCode.Combine(baseHashCode, ReturnTypeInfo, parametersHashCode);
        return hashCode;
    }
}
