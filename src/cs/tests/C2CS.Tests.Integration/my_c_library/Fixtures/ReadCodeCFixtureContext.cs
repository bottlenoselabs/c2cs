// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using C2CS.Tests.Common.Data.Model.C;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class ReadCodeCFixtureContext
{
    private readonly ImmutableDictionary<string, CTestFunction> _functionsByName;
    private readonly ImmutableDictionary<string, CTestEnum> _enumsByName;
    private readonly ImmutableDictionary<string, CTestStruct> _structsByName;

    public string TargetPlatform { get; }

    public ReadCodeCFixtureContext(
        TargetPlatform targetPlatform,
        ImmutableDictionary<string, CTestFunction> functionsByName,
        ImmutableDictionary<string, CTestEnum> enumsByName,
        ImmutableDictionary<string, CTestStruct> structsByName)
    {
        TargetPlatform = targetPlatform.ToString();
        _functionsByName = functionsByName;
        _enumsByName = enumsByName;
        _structsByName = structsByName;
    }

    public CTestFunction GetFunction(string name)
    {
        var exists = _functionsByName.TryGetValue(name, out var value);
        Assert.True(exists, $"The function `{name}` does not exist.");
        return value!;
    }

    public CTestEnum GetEnum(string name)
    {
        var exists = _enumsByName.TryGetValue(name, out var value);
        Assert.True(exists, $"The enum `{name}` does not exist.");
        return value!;
    }

    public CTestStruct GetStruct(string name)
    {
        var exists = _structsByName.TryGetValue(name, out var value);
        Assert.True(exists, $"The struct `{name}` does not exist.");
        AssertStruct(value!);
        return value!;
    }

    private void AssertStruct(CTestStruct value)
    {
        var namesLookup = new List<string>();

        foreach (var field in value.Fields)
        {
            AssertStructField(value.Name, namesLookup, field);
        }
    }

    private void AssertStructField(string structName, List<string> namesLookup, CTestStructField field)
    {
        Assert.False(
            (bool)namesLookup.Contains(field.Name),
            $"C struct `{structName}` already has a field named `{field.Name}`.");
        namesLookup.Add(field.Name);

        Assert.True(
            field.OffsetOf >= 0,
            $"C struct `{structName} field `{field.Name}` has negative offset.");
        Assert.True(
            field.PaddingOf >= 0,
            $"C struct `{structName} field `{field.Name}` has negative padding.");
        Assert.True(
            field.SizeOf > 0,
            $"C struct `{structName} field `{field.Name}` has negative or zero size.");
    }
}
