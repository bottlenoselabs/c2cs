// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using Xunit;

namespace C2CS.Tests.Models;

public sealed class CSharpTestAbstractSyntaxTree(
#pragma warning disable CS9113 // Parameter is unread.
    ImmutableDictionary<string, CSharpTestEnum> enums,
#pragma warning restore CS9113 // Parameter is unread.
    ImmutableDictionary<string, CSharpTestFunction> functions,
#pragma warning disable CS9113 // Parameter is unread.
    ImmutableDictionary<string, CSharpTestMacroObject> macroObjects,
#pragma warning restore CS9113 // Parameter is unread.
#pragma warning disable CS9113 // Parameter is unread.
    ImmutableDictionary<string, CSharpTestStruct> structs)
#pragma warning restore CS9113 // Parameter is unread.
{
    public CSharpTestFunction GetFunction(string name)
    {
        var exists = functions.TryGetValue(name, out var value);
        Assert.True(exists, $"The C# method '{name}' does not exist");
        return value!;
    }
}
