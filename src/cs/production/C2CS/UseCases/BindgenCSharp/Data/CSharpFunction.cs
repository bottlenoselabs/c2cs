// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.UseCases.BindgenCSharp;

public record CSharpFunction(
        string Name,
        string CodeLocationComment,
        CSharpFunctionCallingConvention CallingConvention,
        CSharpType ReturnType,
        ImmutableArray<CSharpFunctionParameter> Parameters)
    : CSharpNode(Name, CodeLocationComment)
{
    public readonly CSharpFunctionCallingConvention CallingConvention = CallingConvention;
    public readonly ImmutableArray<CSharpFunctionParameter> Parameters = Parameters;
    public readonly CSharpType ReturnType = ReturnType;

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return base.ToString();
    }
}
