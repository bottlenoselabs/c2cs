// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Feature.BindgenCSharp.Data;

public sealed class CSharpFunctionPointer : CSharpNode
{
    public readonly ImmutableArray<CSharpFunctionPointerParameter> Parameters;

    public readonly CSharpType ReturnType;

    public CSharpFunctionPointer(
        string name,
        string codeLocationComment,
        int? sizeOf,
        CSharpType returnType,
        ImmutableArray<CSharpFunctionPointerParameter> parameters)
        : base(name, codeLocationComment, sizeOf)
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
}
