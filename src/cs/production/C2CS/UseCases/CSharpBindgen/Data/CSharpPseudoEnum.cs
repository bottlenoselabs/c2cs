// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.UseCases.CSharpBindgen;

public record CSharpPseudoEnum(
    string Name,
    string CodeLocationComment,
    CSharpType IntegerType,
    ImmutableArray<CSharpEnumValue> Values)
    : CSharpNode(Name, CodeLocationComment)
{
    public readonly CSharpType IntegerType = IntegerType;
    public readonly ImmutableArray<CSharpEnumValue> Values = Values;

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return base.ToString();
    }
}
