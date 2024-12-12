// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

[UsedImplicitly]
public sealed class CodeGeneratorNodeFunctionPointer(
    ILogger<CodeGeneratorNodeFunctionPointer> logger,
    NameMapper nameMapper) : CodeGeneratorNodeBase<CFunctionPointer>(logger, nameMapper)
{
    protected override SyntaxNode GenerateCode(
        string nameCSharp, CodeGeneratorDocumentPInvokeContext context, CFunctionPointer node)
    {
        var code = context.IsEnabledFunctionPointers ?
            GenerateCodeFunctionPointer(nameCSharp, node) : GenerateCodeDelegate(nameCSharp, node);
        return ParseMemberCode<StructDeclarationSyntax>(code);
    }

    private string GenerateCodeFunctionPointer(string name, CFunctionPointer node)
    {
        var parameterTypesString = GenerateCodeParameters(node.Parameters, false);
        var parameterTypesAndReturnTypeString = string.IsNullOrEmpty(parameterTypesString) ? node.ReturnType.Name : $"{parameterTypesString}, {node.ReturnType.Name}";

        var code = $$"""

                     [StructLayout(LayoutKind.Sequential)]
                     public struct {{name}}
                     {
                     	public delegate* unmanaged<{{parameterTypesAndReturnTypeString}}> Pointer;

                     	public {{name}}(delegate* unmanaged<{{parameterTypesAndReturnTypeString}}> pointer)
                        {
                            Pointer = pointer;
                        }
                     }

                     """;
        return code;
    }

    private string GenerateCodeDelegate(string name, CFunctionPointer node)
    {
        var parameterTypesString = GenerateCodeParameters(node.Parameters);

        var code = $$"""

                     [StructLayout(LayoutKind.Sequential)]
                     public struct {{name}}
                     {
                         [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
                         public unsafe delegate {{node.ReturnType.Name}} @delegate({{parameterTypesString}});

                         public IntPtr Pointer;

                         public {{name}}(@delegate d)
                         {
                             Pointer = Marshal.GetFunctionPointerForDelegate(d);
                         }
                     }

                     """;
        return code;
    }

    private string GenerateCodeParameters(ImmutableArray<CFunctionPointerParameter> parameters, bool includeNames = true)
    {
        var stringBuilder = new StringBuilder();

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            var parameterTypeNameCSharp = NameMapper.GetTypeNameCSharp(parameter.Type);
            _ = stringBuilder.Append(parameterTypeNameCSharp);

            if (includeNames)
            {
                _ = stringBuilder.Append(' ');
                _ = stringBuilder.Append(parameter.Name);
            }

            var isJoinedWithComma = parameters.Length > 1 && i != parameters.Length - 1;
            if (isJoinedWithComma)
            {
                _ = stringBuilder.Append(',');
            }
        }

        return stringBuilder.ToString();
    }
}
