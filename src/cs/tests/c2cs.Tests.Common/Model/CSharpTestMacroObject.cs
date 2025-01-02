// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Tests.Common.Model;

public class CSharpTestMacroObject
{
    public string Name { get; set; }

    public string TypeName { get; set; }

    public string Value { get; set; }

#pragma warning disable IDE0290
    public CSharpTestMacroObject(FieldDeclarationSyntax syntaxNode, string fieldName)
#pragma warning restore IDE0290
    {
        Name = fieldName;
        TypeName = syntaxNode.Declaration.Type.ToString();
        Value = syntaxNode.Declaration.Variables[0].Initializer!.Value.ToString();
    }

    public override string ToString()
    {
        return Name;
    }
}
