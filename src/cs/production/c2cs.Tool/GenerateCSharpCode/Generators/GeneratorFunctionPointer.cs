// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text;
using bottlenoselabs.Common.Tools;
using c2ffi.Data;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

[UsedImplicitly]
public sealed class GeneratorFunctionPointer(ILogger<GeneratorFunctionPointer> logger)
    : BaseGenerator<CFunctionPointer>(logger)
{
    public override string GenerateCode(CodeGeneratorContext context, string nameCSharp, CFunctionPointer node)
    {
        var code = context.Input.IsEnabledFunctionPointers ?
            GenerateCodeFunctionPointer(context, nameCSharp, node) : GenerateCodeDelegate(context, nameCSharp, node);
        return code;
    }

    private static string GenerateCodeFunctionPointer(
        CodeGeneratorContext context, string nameCSharp, CFunctionPointer node)
    {
        var functionPointerTypeNameCSharp = GetFunctionPointerTypeNameCSharp(context.NameMapper, node);

        var code = $$"""
                     [StructLayout(LayoutKind.Sequential)]
                     public {{(context.Input.IsEnabledRefStructs ? "ref" : string.Empty)}} partial struct {{nameCSharp}}
                     {
                     	public {{functionPointerTypeNameCSharp}} Pointer;

                     	public {{nameCSharp}}({{functionPointerTypeNameCSharp}} pointer)
                        {
                            Pointer = pointer;
                        }
                     }
                     """;
        return code;
    }

    private string GenerateCodeDelegate(
        CodeGeneratorContext context, string name, CFunctionPointer node)
    {
        var parameterTypesString = GenerateCodeParameters(context.NameMapper, node.Parameters);

        var code = $$"""
                     [StructLayout(LayoutKind.Sequential)]
                     public partial {{(context.Input.IsEnabledRefStructs ? "ref" : string.Empty)}} partial struct {{name}}
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

    private static string GetFunctionPointerTypeNameCSharp(
        NameMapper nameMapper,
        CFunctionPointer node)
    {
        var parameterTypesString = GenerateCodeParameters(nameMapper, node.Parameters, false);
        var returnTypeString = nameMapper.GetTypeNameCSharp(node.ReturnType);
        var parameterTypesAndReturnTypeString = string.IsNullOrEmpty(parameterTypesString)
            ? returnTypeString
            : $"{parameterTypesString}, {returnTypeString}";

#pragma warning disable IDE0072
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        var callingConvention = node.CallingConvention switch
#pragma warning restore IDE0072
        {
            CFunctionCallingConvention.Cdecl => "Cdecl",
            CFunctionCallingConvention.FastCall => "Fastcall",
            CFunctionCallingConvention.StdCall => "Stdcall",
            _ => throw new ToolException($"Unknown calling convention for function pointer '{node.Name}'.")
        };

        return $"delegate* unmanaged[{callingConvention}] <{parameterTypesAndReturnTypeString}>";
    }

    private static string GenerateCodeParameters(
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
