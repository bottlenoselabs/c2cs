// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Tests.Common.Model;

public sealed class CSharpTestAbstractSyntaxTree(
#pragma warning disable CS9113 // Parameter is unread.
    ImmutableDictionary<string, CSharpTestEnum> enums,
    ImmutableDictionary<string, CSharpTestFunction> functions,
    ImmutableDictionary<string, CSharpTestMacroObject> macroObjects,
    ImmutableDictionary<string, CSharpTestStruct> structs)
{
    public CSharpTestFunction? TryGetFunction(string name)
    {
        _ = functions.TryGetValue(name, out var value);
        return value;
    }

    public CSharpTestFunction GetFunction(string name)
    {
        var exists = functions.TryGetValue(name, out var value);
        Assert.True(exists, $"The C# method '{name}' does not exist.");
        return value!;
    }

    public CSharpTestEnum? TryGetEnum(string name)
    {
        _ = enums.TryGetValue(name, out var value);
        return value;
    }

    public CSharpTestEnum GetEnum(string name)
    {
        var exists = enums.TryGetValue(name, out var value);
        Assert.True(exists, $"The C# enum '{name}' does not exist.");
        return value!;
    }
}
