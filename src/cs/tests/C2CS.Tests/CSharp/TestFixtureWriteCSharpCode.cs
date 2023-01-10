// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Tests.CSharp.Data;
using C2CS.Tests.CSharp.Data.Models;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace C2CS.Tests.CSharp;

public class TestFixtureWriteCSharpCode
{
    private readonly TestWriteCSharpCodeAbstractSyntaxTree _abstractSyntaxTree;

    public EmitResult EmitResult { get; }

    public TestFixtureWriteCSharpCode(
        EmitResult emitResult,
        TestWriteCSharpCodeAbstractSyntaxTree abstractSyntaxTree)
    {
        EmitResult = emitResult;
        _abstractSyntaxTree = abstractSyntaxTree;
    }

    public CSharpTestFunction GetFunction(string name)
    {
        var exists = _abstractSyntaxTree.Methods.TryGetValue(name, out var value);
        Assert.True(exists, $"The function `{name}` does not exist.");
        return value!;
    }

    public CSharpTestEnum GetEnum(string name)
    {
        var exists = _abstractSyntaxTree.Enums.TryGetValue(name, out var value);
        Assert.True(exists, $"The enum `{name}` does not exist.");
        return value!;
    }

    public CSharpTestStruct GetStruct(string name)
    {
        var exists = _abstractSyntaxTree.Structs.TryGetValue(name, out var value);
        Assert.True(exists, $"The struct `{name}` does not exist.");
        return value!;
    }

    public CSharpTestMacroObject GetMacroObject(string name)
    {
        var exists = _abstractSyntaxTree.MacroObjects.TryGetValue(name, out var value);
        Assert.True(exists, $"The macro object `{name}` does not exist.");
        return value!;
    }
}
