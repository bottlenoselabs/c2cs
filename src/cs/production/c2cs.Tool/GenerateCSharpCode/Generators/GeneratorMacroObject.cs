// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

[UsedImplicitly]
public class GeneratorMacroObject(ILogger<GeneratorMacroObject> logger)
    : BaseGenerator<CMacroObject>(logger)
{
    public override string GenerateCode(CodeGeneratorContext context, string nameCSharp, CMacroObject node)
    {
        var cSharpTypeName = context.NameMapper.GetTypeNameCSharp(node.Type);

        string code;
#pragma warning disable IDE0045
        if (cSharpTypeName == "CString" && node.Value.StartsWith('"') && node.Value.EndsWith('"'))
#pragma warning restore IDE0045
        {
            code = $"""
                    public static readonly {cSharpTypeName} {nameCSharp} = ({cSharpTypeName}){node.Value}u8;
                    """;
        }
        else
        {
            code = $"""
                    public static readonly {cSharpTypeName} {nameCSharp} = ({cSharpTypeName}){node.Value};
                    """;
        }

        return code;
    }
}
