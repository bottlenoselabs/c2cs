// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using bottlenoselabs.Common.Tools;
using C2CS.GenerateCSharpCode.Generators;
using c2ffi.Data.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace C2CS.GenerateCSharpCode;

public sealed class CodeGeneratorContext
{
#pragma warning disable IDE0032
    private readonly NameMapper _nameMapper;
#pragma warning restore IDE0032
    private readonly ImmutableDictionary<Type, BaseGenerator> _nodeCodeGenerators;
    private readonly HashSet<string> _existingNamesCSharp = [];

    public InputSanitized Input { get; }

    public NameMapper NameMapper => _nameMapper;

    public CodeGeneratorContext(
        InputSanitized input,
        ImmutableDictionary<Type, BaseGenerator> nodeCodeGenerators)
    {
        Input = input;
        _nameMapper = new NameMapper(this);
        _nodeCodeGenerators = nodeCodeGenerators;
    }

    internal TMemberDeclarationSyntax? ProcessCNode<TNode, TMemberDeclarationSyntax>(TNode node)
        where TNode : CNode
        where TMemberDeclarationSyntax : MemberDeclarationSyntax
    {
        var type = typeof(TNode);
        if (!_nodeCodeGenerators.TryGetValue(type, out var codeGenerator))
        {
            throw new ToolException($"A code generator '{nameof(BaseGenerator)}' does not exist for the type '{type.FullName ?? type.Name}'.");
        }

        var nameCSharp = _nameMapper.GetNodeNameCSharp(node);
        var isAlreadyAdded = !_existingNamesCSharp.Add(nameCSharp);
        if (isAlreadyAdded)
        {
            return null;
        }

        var code = codeGenerator.GenerateCode(nameCSharp, this, node);
        var memberDeclarationSyntax = SyntaxFactory.ParseMemberDeclaration(code.Trim())!;
        if (memberDeclarationSyntax is not TMemberDeclarationSyntax typedMemberDeclarationSyntax)
        {
            throw new ToolException($"Generated code can be parsed into a {typeof(TMemberDeclarationSyntax)}.");
        }

        var memberDeclarationSyntaxWithTrivia = typedMemberDeclarationSyntax
            .WithLeadingTrivia(SyntaxFactory.LineFeed)
            .WithTrailingTrivia(SyntaxFactory.LineFeed);

        return memberDeclarationSyntaxWithTrivia;
    }
}
