// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.WriteCodeCSharp.Data.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Contexts.WriteCodeCSharp.Domain.CodeGenerator;

public class CSharpCodeGeneratorContext
{
    public ImmutableArray<MemberDeclarationSyntax>.Builder Members { get; }

    public ImmutableDictionary<string, CSharpStruct> StructsByName { get; }

    public CSharpCodeGeneratorContext(ImmutableArray<CSharpStruct> structs)
    {
        Members = ImmutableArray.CreateBuilder<MemberDeclarationSyntax>();
        StructsByName = structs.ToImmutableDictionary(x => x.Name);
    }
}
