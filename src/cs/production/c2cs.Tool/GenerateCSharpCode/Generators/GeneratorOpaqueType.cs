// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

[UsedImplicitly]
public class GeneratorNodeOpaqueType(ILogger<GeneratorNodeOpaqueType> logger)
    : BaseGenerator<COpaqueType>(logger)
{
    protected override string GenerateCode(
        string nameCSharp, CodeGeneratorContext context, COpaqueType node)
    {
        var code = $$"""
                     [StructLayout(LayoutKind.Sequential)]
                     public struct {{nameCSharp}}
                     {
                     }
                     """;

        return code;
    }
}
