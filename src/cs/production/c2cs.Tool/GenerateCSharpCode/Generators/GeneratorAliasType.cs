// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

[UsedImplicitly]
public class GeneratorAliasType(ILogger<GeneratorAliasType> logger)
    : BaseGenerator<CTypeAlias>(logger)
{
    public override string? GenerateCode(CodeGeneratorContext context, string nameCSharp, CTypeAlias node)
    {
        var underlyingTypeNameCSharp = context.NameMapper.GetTypeNameCSharp(node.UnderlyingType);
        var sizeOf = node.UnderlyingType.SizeOf;
        var alignOf = node.UnderlyingType.AlignOf;

        if (node.UnderlyingType.NodeKind == CNodeKind.FunctionPointer)
        {
            var functionPointer = context.Ffi.FunctionPointers[node.UnderlyingType.Name];
            var functionPointerCodeGenerator = context.GetCodeGenerator<CFunctionPointer>();
            var functionPointerCode = functionPointerCodeGenerator.GenerateCode(
                context, nameCSharp, functionPointer);
            return functionPointerCode;
        }

        var code = $$"""
                     [StructLayout(LayoutKind.Sequential, Size = {{sizeOf}}, Pack = {{alignOf}})]
                     public {{(context.Input.IsEnabledRefStructs ? "ref" : string.Empty)}} partial struct {{nameCSharp}}
                     {
                        public {{underlyingTypeNameCSharp}} Data;

                     	public static implicit operator {{underlyingTypeNameCSharp}}({{nameCSharp}} data) => data.Data;
                     	public static implicit operator {{nameCSharp}}({{underlyingTypeNameCSharp}} data) => new {{nameCSharp}}() {Data = data};
                     }
                     """;

        return code;
    }
}
