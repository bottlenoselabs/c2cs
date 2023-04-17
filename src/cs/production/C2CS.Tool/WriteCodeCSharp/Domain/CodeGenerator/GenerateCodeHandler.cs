// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using C2CS.WriteCodeCSharp.Data;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace C2CS.WriteCodeCSharp.Domain.CodeGenerator;

public abstract class GenerateCodeHandler
{
    private readonly ILogger<GenerateCodeHandler> _logger;

    protected GenerateCodeHandler(
        ILogger<GenerateCodeHandler> logger)
    {
        _logger = logger;
    }

    protected internal abstract SyntaxNode? GenerateCode(CSharpCodeGeneratorContext context, object obj);
}

public abstract class GenerateCodeHandler<TNode> : GenerateCodeHandler
    where TNode : CSharpNode
{
    protected GenerateCodeHandler(
        ILogger<GenerateCodeHandler<TNode>> logger)
        : base(logger)
    {
    }

    protected internal override SyntaxNode? GenerateCode(CSharpCodeGeneratorContext context, object obj)
    {
        var node = obj as TNode;
        if (node != null)
        {
            return GenerateCode(context, node);
        }

        var objType = obj.GetType();
        var instanceTypeString = objType.FullName ?? objType.Name;
        throw new InvalidOperationException($"The instance type is null for type '{instanceTypeString}'");
    }

    protected abstract SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, TNode node);
}
