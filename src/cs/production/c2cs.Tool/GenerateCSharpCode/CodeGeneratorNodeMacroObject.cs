// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

[UsedImplicitly]
public class CodeGeneratorNodeMacroObject(ILogger<CodeGeneratorNodeMacroObject> logger)
    : CodeGeneratorNode<CMacroObject>(logger)
{
    protected override string GenerateCode(
        string nameCSharp, CodeGeneratorContext context, CMacroObject node)
    {
        var cSharpTypeName = context.NameMapper.GetTypeNameCSharp(node.Type);
        var code = $"""
                    public static {cSharpTypeName} {nameCSharp} = ({cSharpTypeName}){node.Value};
                    """;

        return code;
    }
}
