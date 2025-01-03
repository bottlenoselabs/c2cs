// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Tests.Common.Model;

public class CSharpTestStruct
{
    public string Name { get; }

    public CSharpTestStructLayout Layout { get; }

    public ImmutableArray<CSharpTestStructField> Fields { get; }

    public CSharpTestStruct(StructDeclarationSyntax syntaxNode)
    {
        Name = syntaxNode.Identifier.Text;
        Layout = new CSharpTestStructLayout(syntaxNode);
        Fields = CreateTestStructFields(syntaxNode, Layout);
    }

    public override string ToString()
    {
        return Name;
    }

    private ImmutableArray<CSharpTestStructField> CreateTestStructFields(
        StructDeclarationSyntax syntaxNode,
        CSharpTestStructLayout layout)
    {
        var builder = ImmutableArray.CreateBuilder<CSharpTestStructField>();

        foreach (var syntaxNodeMember in syntaxNode.Members)
        {
            if (syntaxNodeMember is not FieldDeclarationSyntax syntaxNodeField)
            {
                continue;
            }

            var field = new CSharpTestStructField(syntaxNodeField, layout);
            builder.Add(field);
        }

        return builder.ToImmutable();
    }
}
