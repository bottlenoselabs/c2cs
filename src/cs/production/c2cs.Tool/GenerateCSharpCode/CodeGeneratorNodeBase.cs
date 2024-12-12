// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using bottlenoselabs.Common.Tools;
using c2ffi.Data.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

public abstract class CodeGeneratorNodeBase
{
    protected readonly ILogger<CodeGeneratorNodeBase> Logger;
    protected readonly NameMapper NameMapper;

    internal CodeGeneratorNodeBase(
        ILogger<CodeGeneratorNodeBase> logger,
        NameMapper nameMapper)
    {
        Logger = logger;
        NameMapper = nameMapper;
    }

    protected internal abstract SyntaxNode? GenerateCode(
        CodeGeneratorDocumentPInvokeContext context,
        object obj);

    protected T ParseMemberCode<T>(string code)
        where T : MemberDeclarationSyntax
    {
        var member = SyntaxFactory.ParseMemberDeclaration(code)!;
        if (member is T syntax)
        {
            return syntax;
        }

        var up = new ToolException($"Error generating C# code for {typeof(T).Name}.");
        throw up;
    }
}

public abstract class CodeGeneratorNodeBase<TNode>(
    ILogger<CodeGeneratorNodeBase<TNode>> logger,
    NameMapper nameMapper) : CodeGeneratorNodeBase(logger, nameMapper)
    where TNode : CNode
{
    protected internal override SyntaxNode? GenerateCode(
        CodeGeneratorDocumentPInvokeContext context,
        object obj)
    {
        var node = obj as TNode;
        if (node != null)
        {
            var name = NameMapper.GetNodeNameCSharp(node);
            var alreadyAdded = context.NameAlreadyExists(name);
            if (alreadyAdded)
            {
                return null;
            }

            return GenerateCode(name, context, node);
        }

        var objType = obj.GetType();
        var instanceTypeString = objType.FullName ?? objType.Name;
        throw new InvalidOperationException($"The instance type is null for type '{instanceTypeString}'");
    }

    protected abstract SyntaxNode? GenerateCode(
        string nameCSharp,
        CodeGeneratorDocumentPInvokeContext context,
        TNode node);
}
