// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.UseCases.BindgenCSharp;

public record CSharpPseudoEnum : CSharpNode
{
    public readonly CSharpType IntegerType;
    public readonly ImmutableArray<CSharpEnumValue> Values;

    public CSharpPseudoEnum(
        string name,
        string codeLocationComment,
        CSharpType integerType,
        ImmutableArray<CSharpEnumValue> values)
        : base(name, codeLocationComment)
    {
        IntegerType = integerType;
        Values = values;
    }

    // Required for debugger string with records
    // ReSharper disable once RedundantOverriddenMember
    public override string ToString()
    {
        return base.ToString();
    }
}
