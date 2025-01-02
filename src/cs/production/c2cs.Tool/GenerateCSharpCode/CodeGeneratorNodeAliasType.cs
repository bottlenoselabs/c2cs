// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

[UsedImplicitly]
public class CodeGeneratorNodeAliasType(ILogger<CodeGeneratorNodeAliasType> logger)
    : CodeGeneratorNode<CTypeAlias>(logger)
{
    protected override string GenerateCode(
        string nameCSharp, CodeGeneratorContext context, CTypeAlias node)
    {
        var underlyingTypeNameCSharp = context.NameMapper.GetTypeNameCSharp(node.UnderlyingType);
        var sizeOf = node.UnderlyingType.SizeOf;
        var alignOf = node.UnderlyingType.AlignOf;

        var code = $$"""
                     [StructLayout(LayoutKind.Explicit, Size = {{sizeOf}}, Pack = {{alignOf}})]
                     public struct {{nameCSharp}}
                     {
                        [FieldOffset(0)]
                        public {{underlyingTypeNameCSharp}} Data;

                     	public static implicit operator {{underlyingTypeNameCSharp}}({{nameCSharp}} data) => data.Data;
                     	public static implicit operator {{nameCSharp}}({{underlyingTypeNameCSharp}} data) => new {{nameCSharp}}() {Data = data};
                     }
                     """;

        return code;
    }
}
