// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Tests.Common.Model;

public class CSharpTestEnumValue
{
    public string Name { get; }

    public string Value { get; }

#pragma warning disable IDE0290
    public CSharpTestEnumValue(EnumMemberDeclarationSyntax syntaxNode)
#pragma warning restore IDE0290
    {
        Name = syntaxNode.Identifier.ValueText;
        Value = syntaxNode.EqualsValue!.Value.GetText().ToString().Trim();
    }

    public override string ToString()
    {
        return Name;
    }
}
