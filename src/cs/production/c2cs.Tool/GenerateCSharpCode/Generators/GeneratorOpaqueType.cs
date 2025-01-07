// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using c2ffi.Data.Nodes;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.GenerateCSharpCode.Generators;

[UsedImplicitly]
public class GeneratorOpaqueType(ILogger<GeneratorOpaqueType> logger)
    : BaseGenerator<COpaqueType>(logger)
{
    public override string GenerateCode(CodeGeneratorContext context, string nameCSharp, COpaqueType node)
    {
        var code = $$"""
                     [StructLayout(LayoutKind.Sequential)]
                     public partial struct {{nameCSharp}}
                     {
                     }
                     """;

        return code;
    }
}
