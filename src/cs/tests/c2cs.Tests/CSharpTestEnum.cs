// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Tests;
#pragma warning disable CA1036

[PublicAPI]
public class CSharpTestEnum
{
    public string Name { get; }

    public string BaseType { get; }

    public int SizeOf { get; }

    public ImmutableArray<CSharpTestEnumValue> Values { get; set; }

    public CSharpTestEnum(SemanticModel semanticModel, EnumDeclarationSyntax syntaxNode)
    {
        var typeSyntax = syntaxNode.BaseList!.Types.FirstOrDefault()!.Type;
        SizeOf = semanticModel.GetTypeInfo(typeSyntax).Type!.SizeOf()!.Value;
        Name = syntaxNode.Identifier.Text;
        BaseType = syntaxNode.BaseList!.Types[0].Type.ToString();

        var builder = ImmutableArray.CreateBuilder<CSharpTestEnumValue>();
        foreach (var syntaxNodeEnumMember in syntaxNode.Members)
        {
            var enumValue = new CSharpTestEnumValue(syntaxNodeEnumMember);
            builder.Add(enumValue);
        }

        Values = builder.ToImmutable();
    }

    public override string ToString()
    {
        return Name;
    }
}
