// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using bottlenoselabs.Common.Tools;
using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode;

[UsedImplicitly]
public class CodeGeneratorNodeEnum(ILogger<CodeGeneratorNodeEnum> logger)
    : CodeGeneratorNode<CEnum>(logger)
{
    protected override string GenerateCode(
        string nameCSharp,
        CodeGeneratorContext context,
        CEnum node)
    {
        var integerTypeNameCSharp = node.SizeOf switch
        {
            1 => "sbyte",
            2 => "short",
            4 => "int",
            8 => "long",
            _ => throw new ToolException(
                $"The enum size is not supported: '{nameCSharp}' of size {node.SizeOf}.")
        };

        var enumValueCodes = GenerateCodeEnumValues(node.Values);
        var enumValuesCode = string.Join(",\n", enumValueCodes);

        var code = $$"""

                     public enum {{nameCSharp}} : {{integerTypeNameCSharp}}
                         {
                             {{enumValuesCode}}
                         }

                     """;

        return code;
    }

    private static string[] GenerateCodeEnumValues(ImmutableArray<CEnumValue> enumValues)
    {
        var builder = ImmutableArray.CreateBuilder<string>(enumValues.Length);

        foreach (var enumValue in enumValues)
        {
            var code = $"{enumValue.Name} = {enumValue.Value}";
            builder.Add(code);
        }

        return builder.ToArray();
    }
}
