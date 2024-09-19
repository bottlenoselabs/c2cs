// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using Xunit;

namespace C2CS.Tests.Models;

public sealed class CSharpTestAbstractSyntaxTree
{
    private readonly ImmutableDictionary<string, CSharpTestEnum> _enums;
    private readonly ImmutableDictionary<string, CSharpTestFunction> _functions;
    private readonly ImmutableDictionary<string, CSharpTestMacroObject> _macroObjects;
    private readonly ImmutableDictionary<string, CSharpTestStruct> _structs;

    public CSharpTestAbstractSyntaxTree(
        ImmutableDictionary<string, CSharpTestEnum> enums,
        ImmutableDictionary<string, CSharpTestFunction> functions,
        ImmutableDictionary<string, CSharpTestMacroObject> macroObjects,
        ImmutableDictionary<string, CSharpTestStruct> structs)
    {
        _enums = enums;
        _functions = functions;
        _macroObjects = macroObjects;
        _structs = structs;
    }

    public CSharpTestFunction GetFunction(string name)
    {
        var exists = _functions.TryGetValue(name, out var value);
        Assert.True(exists, $"The C# method '{name}' does not exist");
        return value!;
    }
}
