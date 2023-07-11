// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Reflection;
using Microsoft.CodeAnalysis.Emit;

namespace C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator;

public class CSharpLibraryCompilerResult
{
    public readonly EmitResult EmitResult;
    public readonly Assembly Assembly;

    public CSharpLibraryCompilerResult(
        EmitResult emitResult,
        Assembly assembly)
    {
        EmitResult = emitResult;
        Assembly = assembly;
    }
}
