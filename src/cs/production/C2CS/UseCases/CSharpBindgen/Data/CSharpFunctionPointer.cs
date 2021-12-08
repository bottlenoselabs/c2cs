// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.UseCases.CSharpBindgen;

public record CSharpFunctionPointer(
    string Name,
    string CodeLocationComment,
    CSharpType ReturnType,
    ImmutableArray<CSharpFunctionPointerParameter> Parameters)
    : CSharpNode(Name, CodeLocationComment)
{
    public readonly CSharpType ReturnType = ReturnType;
    public readonly ImmutableArray<CSharpFunctionPointerParameter> Parameters = Parameters;

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return base.ToString();
    }
}
