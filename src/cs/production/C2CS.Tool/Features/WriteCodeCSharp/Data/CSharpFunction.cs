// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using C2CS.Foundation;

namespace C2CS.Features.WriteCodeCSharp.Data;

public sealed class CSharpFunction : CSharpNode
{
    public readonly CSharpFunctionCallingConvention CallingConvention;

    public readonly ImmutableArray<CSharpFunctionParameter> Parameters;

    public readonly CSharpTypeInfo ReturnTypeInfo;

    public CSharpFunction(
        string name,
        string className,
        int? sizeOf,
        CSharpFunctionCallingConvention callingConvention,
        CSharpTypeInfo returnTypeInfo,
        ImmutableArray<CSharpFunctionParameter> parameters,
        ImmutableArray<Attribute> attributes)
        : base(name, className, sizeOf, attributes)
    {
        CallingConvention = callingConvention;
        ReturnTypeInfo = returnTypeInfo;
        Parameters = parameters;
    }

    public override bool Equals(CSharpNode? other)
    {
        if (!base.Equals(other) || other is not CSharpFunction other2)
        {
            return false;
        }

        var result = CallingConvention == other2.CallingConvention &&
                     ReturnTypeInfo == other2.ReturnTypeInfo &&
                     Parameters.SequenceEqual(other2.Parameters);

        return result;
    }

    public override int GetHashCode()
    {
        var baseHashCode = base.GetHashCode();
        var parameters = Parameters.GetHashCodeMembers();
        var hashCode = HashCode.Combine(baseHashCode, CallingConvention, ReturnTypeInfo, parameters);
        return hashCode;
    }
}
