// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.WriteCodeCSharp.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.WriteCodeCSharp.CodeGenerator.Generators;

public class ConstantCodeGenerator : GenerateCodeHandler<CSharpConstant>
{
    public ConstantCodeGenerator(
        ILogger<ConstantCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpConstant node)
    {
        var code = $"""

                    public const {node.Type} {node.Name} = {node.Value};

                    """;

        var member = context.ParseMemberCode<FieldDeclarationSyntax>(code);
        return member;
    }
}
