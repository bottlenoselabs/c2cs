// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data.Nodes;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

public abstract class BaseGenerator<TNode>(ILogger<BaseGenerator<TNode>> logger)
    where TNode : CNode
{
    protected readonly ILogger<BaseGenerator<TNode>> Logger = logger;

    public abstract string? GenerateCode(CodeGeneratorContext context, string nameCSharp, TNode node);
}
