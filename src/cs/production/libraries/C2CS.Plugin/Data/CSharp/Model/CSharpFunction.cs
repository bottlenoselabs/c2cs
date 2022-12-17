// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Data.CSharp.Model;

public sealed class CSharpFunction : CSharpNode
{
    public readonly CSharpFunctionCallingConvention CallingConvention;

    public readonly ImmutableArray<CSharpFunctionParameter> Parameters;

    public readonly CSharpTypeInfo ReturnTypeInfo;

    public CSharpFunction(
        ImmutableArray<TargetPlatform> platforms,
        string name,
        string cKind,
        string cCodeLocation,
        int? sizeOf,
        CSharpFunctionCallingConvention callingConvention,
        CSharpTypeInfo returnTypeInfo,
        ImmutableArray<CSharpFunctionParameter> parameters,
        ImmutableArray<Attribute> attributes)
        : base(platforms, name, cKind, cCodeLocation, sizeOf, attributes)
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
