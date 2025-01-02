// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data.Nodes;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

public abstract class CodeGeneratorNode(ILogger<CodeGeneratorNode> logger)
{
    protected readonly ILogger<CodeGeneratorNode> Logger = logger;

    protected internal abstract string GenerateCode(
        string nameCSharp,
        CodeGeneratorContext context,
        object obj);
}

public abstract class CodeGeneratorNode<TNode>(ILogger<CodeGeneratorNode<TNode>> logger)
    : CodeGeneratorNode(logger)
    where TNode : CNode
{
    protected internal override string GenerateCode(
        string nameCSharp,
        CodeGeneratorContext context,
        object obj)
    {
        var node = (TNode)obj;
        var code = GenerateCode(nameCSharp, context, node);
        return code;
    }

    protected abstract string GenerateCode(string nameCSharp, CodeGeneratorContext context, TNode node);
}
