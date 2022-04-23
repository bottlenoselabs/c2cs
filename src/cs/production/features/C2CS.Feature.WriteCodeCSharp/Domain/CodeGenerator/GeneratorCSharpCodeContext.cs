// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.WriteCodeCSharp.Data.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Feature.WriteCodeCSharp.Domain.CodeGenerator;

public class GeneratorCSharpCodeContext
{
    public ImmutableArray<MemberDeclarationSyntax>.Builder Members { get; }

    public ImmutableDictionary<string, CSharpStruct> StructsByName { get; }

    public GeneratorCSharpCodeContext(ImmutableArray<CSharpStruct> structs)
    {
        Members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();
        StructsByName = structs.ToImmutableDictionary(x => x.Name);
    }
}
