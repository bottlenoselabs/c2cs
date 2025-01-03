// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

[UsedImplicitly]
public sealed class GeneratorFunctionPointer(ILogger<GeneratorFunctionPointer> logger)
    : BaseGenerator<CFunctionPointer>(logger)
{
    protected override string GenerateCode(
        string nameCSharp, CodeGeneratorContext context, CFunctionPointer node)
    {
        var nameMapper = context.NameMapper;
        var code = context.Input.IsEnabledFunctionPointers ?
            GenerateCodeFunctionPointer(nameMapper, nameCSharp, node) : GenerateCodeDelegate(nameMapper, nameCSharp, node);
        return code;
    }

    private string GenerateCodeFunctionPointer(
        NameMapper nameMapper, string name, CFunctionPointer node)
    {
        var parameterTypesString = GenerateCodeParameters(nameMapper, node.Parameters, false);
        var returnTypeString = nameMapper.GetTypeNameCSharp(node.ReturnType);
        var parameterTypesAndReturnTypeString = string.IsNullOrEmpty(parameterTypesString) ? returnTypeString : $"{parameterTypesString}, {returnTypeString}";

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

    private string GenerateCodeDelegate(
        NameMapper nameMapper, string name, CFunctionPointer node)
    {
        var parameterTypesString = GenerateCodeParameters(nameMapper, node.Parameters);

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

    private string GenerateCodeParameters(
        NameMapper nameMapper, ImmutableArray<CFunctionPointerParameter> parameters, bool includeNames = true)
    {
        var stringBuilder = new StringBuilder();

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            var parameterTypeNameCSharp = nameMapper.GetTypeNameCSharp(parameter.Type);
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
