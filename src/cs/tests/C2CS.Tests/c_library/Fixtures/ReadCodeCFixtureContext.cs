// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Explore;
using C2CS.Contexts.ReadCodeC.Parse;
using C2CS.Tests.Common.Data.Model.C;
using JetBrains.Annotations;
using Xunit;

namespace C2CS.IntegrationTests.c_library.Fixtures;

[PublicAPI]
public sealed class ReadCodeCFixtureContext
{
    private readonly ImmutableDictionary<string, CTestEnum> _enums;
    private readonly ImmutableDictionary<string, CTestFunction> _functions;
    private readonly ImmutableDictionary<string, CTestMacroObject> _macroObjects;
    private readonly ImmutableDictionary<string, CTestRecord> _records;

    public ReadCodeCFixtureContext(
        ExploreOptions exploreOptions,
        ParseOptions parseOptions,
        TargetPlatform targetPlatformRequested,
        TargetPlatform targetPlatformActual,
        ImmutableDictionary<string, CTestFunction> functions,
        ImmutableDictionary<string, CTestEnum> enums,
        ImmutableDictionary<string, CTestRecord> records,
        ImmutableDictionary<string, CTestMacroObject> macroObjectsByName)
    {
        ExploreOptions = exploreOptions;
        ParseOptions = parseOptions;
        TargetPlatformRequested = targetPlatformRequested.ToString();
        TargetPlatformActual = targetPlatformActual.ToString();
        _functions = functions;
        _enums = enums;
        _records = records;
        _macroObjects = macroObjectsByName;

        AssertPInvokePlatformNameFunction();
    }

    public string TargetPlatformRequested { get; }

    public string TargetPlatformActual { get; }

    public ExploreOptions ExploreOptions { get; }

    public ParseOptions ParseOptions { get; }

    public CTestFunction GetFunction(string name)
    {
        var exists = _functions.TryGetValue(name, out var value);
        Assert.True(exists, $"The function `{name}` does not exist.");
        return value!;
    }

    public CTestFunction? TryGetFunction(string name)
    {
        var exists = _functions.TryGetValue(name, out var value);
        return exists ? value : null;
    }

    public CTestEnum GetEnum(string name)
    {
        var exists = _enums.TryGetValue(name, out var value);
        Assert.True(exists, $"The enum `{name}` does not exist: {TargetPlatformRequested}");
        return value!;
    }

    public CTestEnum? TryGetEnum(string name)
    {
        var exists = _enums.TryGetValue(name, out var value);
        return exists ? value : null;
    }

    public CTestRecord GetRecord(string name)
    {
        var exists = _records.TryGetValue(name, out var value);
        Assert.True(exists, $"The record `{name}` does not exist: {TargetPlatformRequested}");
        AssertRecord(value!);
        return value!;
    }

    public CTestRecord? TryGetRecord(string name)
    {
        var exists = _records.TryGetValue(name, out var value);
        if (!exists)
        {
            return null;
        }

        AssertRecord(value!);
        return value;
    }

    public CTestMacroObject GetMacroObject(string name)
    {
        var exists = _macroObjects.TryGetValue(name, out var value);
        Assert.True(exists, $"The macro object `{name}` does not exist: {TargetPlatformRequested}");
        return value!;
    }

    public CTestMacroObject? TryGetMacroObject(string name)
    {
        var exists = _macroObjects.TryGetValue(name, out var value);
        return exists ? value : null;
    }

    private void AssertRecord(CTestRecord record)
    {
        var namesLookup = new List<string>();

        foreach (var field in record.Fields)
        {
            AssertRecordField(record, field, namesLookup);
        }

        Assert.True(
            record.AlignOf > 0,
            $"C record `{record.Name} does not have an alignment of which is positive.");

        Assert.True(
            record.SizeOf >= 0,
            $"C record `{record.SizeOf} does not have an size of of which is positive or zero.");
    }

    private void AssertRecordField(CTestRecord record, CTestRecordField field, List<string> namesLookup)
    {
        var recordKindName = record.IsUnion ? "union" : "struct";

        Assert.False(
            namesLookup.Contains(field.Name),
            $"C {recordKindName} `{record.Name}` already has a field named `{field.Name}`.");
        namesLookup.Add(field.Name);

        Assert.True(
            field.OffsetOf >= 0,
            $"C {recordKindName} `{record.Name} field `{field.Name}` does not have an offset of which is positive or zero.");
        Assert.True(
            field.SizeOf > 0,
            $"C {recordKindName} `{record.Name} field `{field.Name}` does not have a size of which is positive.");

        if (record.IsUnion)
        {
            Assert.True(
                field.OffsetOf == 0,
                $"C union `{record.Name} field `{field.Name}` does not have an offset of zero.");
            Assert.True(
                field.SizeOf == record.SizeOf,
                $"C union `{record.Name} field `{field.Name}` does not have a size that matches the union.");
        }
    }

    private void AssertPInvokePlatformNameFunction()
    {
        var function = GetFunction("pinvoke_get_platform_name");
        Assert.Equal("cdecl", function.CallingConvention);
        Assert.Equal("char*", function.ReturnTypeName);
        Assert.True(function.Parameters.IsDefaultOrEmpty);
    }
}
