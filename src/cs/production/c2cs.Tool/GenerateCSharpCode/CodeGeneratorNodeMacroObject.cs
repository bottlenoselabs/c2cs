// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

[UsedImplicitly]
public class CodeGeneratorNodeMacroObject(
    ILogger<CodeGeneratorNodeMacroObject> logger,
    NameMapper nameMapper) : CodeGeneratorNodeBase<CMacroObject>(logger, nameMapper)
{
    protected override SyntaxNode GenerateCode(
        string nameCSharp, CodeGeneratorDocumentPInvokeContext context, CMacroObject node)
    {
        var code = $"""

                    public static {node.Type} {nameCSharp} = ({node.Type}){node.Value};

                    """;

        var member = ParseMemberCode<FieldDeclarationSyntax>(code);
        return member;
    }
}
