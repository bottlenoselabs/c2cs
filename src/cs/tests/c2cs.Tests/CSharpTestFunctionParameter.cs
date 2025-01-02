// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.Tests;

public class CSharpTestFunctionParameter
{
    public string Name { get; }

    public CSharpTestType Type { get; }

#pragma warning disable IDE0290
    public CSharpTestFunctionParameter(SemanticModel semanticModel, ParameterSyntax syntaxNode)
#pragma warning restore IDE0290
    {
        Name = syntaxNode.Identifier.ValueText;
        Type = new CSharpTestType(semanticModel.GetTypeInfo(syntaxNode.Type!).Type!);
    }

    public override string ToString()
    {
        return Name;
    }
}
