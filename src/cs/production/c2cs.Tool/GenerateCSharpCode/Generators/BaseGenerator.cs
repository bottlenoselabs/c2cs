// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data.Nodes;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

public abstract class BaseGenerator(ILogger<BaseGenerator> logger)
{
    protected readonly ILogger<BaseGenerator> Logger = logger;

    protected internal abstract string GenerateCode(
        string nameCSharp,
        CodeGeneratorContext context,
        object obj);
}

public abstract class BaseGenerator<TNode>(ILogger<BaseGenerator<TNode>> logger)
    : BaseGenerator(logger)
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
