// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace C2CS.Tests.Foundation.Roslyn;

public class CSharpCodeBuildResult
{
    public EmitResult EmitResult { get; }

    public Type ClassType { get; }

    public CSharpCodeBuildResult(
        EmitResult emitResult,
        Assembly assembly)
    {
        EmitResult = emitResult;

        const string className = "bottlenoselabs._container_library";
        var classType = assembly.GetType(className);
        Assert.True(classType != null, $"The class `{className}` does not exist.");
        ClassType = classType!;
    }
}
