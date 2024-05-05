// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Commands.WriteCodeCSharp.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator.Generators;

public class MacroCodeGenerator : GenerateCodeHandler<CSharpMacroObject>
{
    public MacroCodeGenerator(
        ILogger<MacroCodeGenerator> logger)
        : base(logger)
    {
    }

    protected override SyntaxNode GenerateCode(CSharpCodeGeneratorContext context, CSharpMacroObject node)
    {
        var name = node.Name;
        if (context.NameAlreadyExists(node.FullName))
        {
            name = "_" + name;
        }

        string code;
        if (node.IsConstant)
        {
            code = $@"
public const {node.Type} {name} = {node.Value};
";
        }
        else
        {
            code = $@"
public static {node.Type} {name} = ({node.Type}){node.Value};
";
        }

        var member = context.ParseMemberCode<FieldDeclarationSyntax>(code);
        return member;
    }
}
