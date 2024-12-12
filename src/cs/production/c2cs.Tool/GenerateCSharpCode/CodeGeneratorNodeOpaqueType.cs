// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

[UsedImplicitly]
public class CodeGeneratorNodeOpaqueType(
    ILogger<CodeGeneratorNodeOpaqueType> logger,
    NameMapper nameMapper) : CodeGeneratorNodeBase<COpaqueType>(logger, nameMapper)
{
    protected override SyntaxNode GenerateCode(
        string nameCSharp, CodeGeneratorDocumentPInvokeContext context, COpaqueType node)
    {
        var code = $$"""

                     [StructLayout(LayoutKind.Sequential)]
                     public struct {{nameCSharp}}
                     {
                     }

                     """;

        return ParseMemberCode<StructDeclarationSyntax>(code);
    }
}
