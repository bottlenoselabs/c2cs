// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Tests.Common.Data.Model.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace C2CS.Tests.test_c_library.Fixtures.CSharp;

public class WriteCSharpCodeFixtureContext
{
    private readonly ImmutableDictionary<string, CSharpTestEnum> _enums;
    private readonly ImmutableDictionary<string, CSharpTestFunction> _functions;
    private readonly ImmutableDictionary<string, CSharpTestMacroObject> _macroObjects;
    private readonly ImmutableDictionary<string, CSharpTestStruct> _structs;

    public WriteCSharpCodeFixtureContext(
        EmitResult emitResult,
        ImmutableDictionary<string, CSharpTestFunction> functions,
        ImmutableDictionary<string, CSharpTestEnum> enums,
        ImmutableDictionary<string, CSharpTestStruct> structs,
        ImmutableDictionary<string, CSharpTestMacroObject> macroObjects)
    {
        EmitResult = emitResult;
        _functions = functions;
        _enums = enums;
        _structs = structs;
        _macroObjects = macroObjects;
    }

    public EmitResult EmitResult { get; }

    public CSharpTestFunction GetFunction(string name)
    {
        var exists = _functions.TryGetValue(name, out var value);
        Assert.True(exists, $"The function `{name}` does not exist.");
        return value!;
    }

    public CSharpTestEnum GetEnum(string name)
    {
        var exists = _enums.TryGetValue(name, out var value);
        Assert.True(exists, $"The enum `{name}` does not exist.");
        return value!;
    }

    public CSharpTestStruct GetStruct(string name)
    {
        var exists = _structs.TryGetValue(name, out var value);
        Assert.True(exists, $"The struct `{name}` does not exist.");
        return value!;
    }

    public CSharpTestMacroObject GetMacroObject(string name)
    {
        var exists = _macroObjects.TryGetValue(name, out var value);
        Assert.True(exists, $"The macro object `{name}` does not exist.");
        return value!;
    }
}
