// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Tests.Common.Data.Model.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public class WriteCodeCSharpFixtureContext
{
    private readonly ImmutableDictionary<string, CSharpTestFunction> _testFunctions;
    private readonly ImmutableDictionary<string, CSharpTestEnum> _testEnums;
    private readonly ImmutableDictionary<string, CSharpTestStruct> _testStructs;

    public EmitResult EmitResult { get; }

    public WriteCodeCSharpFixtureContext(
        EmitResult emitResult,
        ImmutableDictionary<string, CSharpTestFunction> testFunctions,
        ImmutableDictionary<string, CSharpTestEnum> testEnums,
        ImmutableDictionary<string, CSharpTestStruct> testStructs)
    {
        EmitResult = emitResult;
        _testFunctions = testFunctions;
        _testEnums = testEnums;
        _testStructs = testStructs;
    }

    public CSharpTestFunction GetFunction(string name)
    {
        var exists = _testFunctions.TryGetValue(name, out var value);
        Assert.True(exists, $"The function `{name}` does not exist.");
        return value!;
    }

    public CSharpTestEnum GetEnum(string name)
    {
        var exists = _testEnums.TryGetValue(name, out var value);
        Assert.True(exists, $"The enum `{name}` does not exist.");
        return value!;
    }

    public CSharpTestStruct GetStruct(string name)
    {
        var exists = _testStructs.TryGetValue(name, out var value);
        Assert.True(exists, $"The struct `{name}` does not exist.");
        return value!;
    }
}
