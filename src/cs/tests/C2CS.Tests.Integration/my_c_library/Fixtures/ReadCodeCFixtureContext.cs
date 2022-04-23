// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using C2CS.Tests.Common.Data.Model.C;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library.Fixtures;

public sealed class ReadCodeCFixtureContext
{
    private readonly ImmutableDictionary<string, CTestFunction> _functionsByName;
    private readonly ImmutableDictionary<string, CTestEnum> _enumsByName;
    private readonly ImmutableDictionary<string, CTestRecord> _recordsByName;

    public string TargetPlatform { get; }

    public ReadCodeCFixtureContext(
        TargetPlatform targetPlatform,
        ImmutableDictionary<string, CTestFunction> functionsByName,
        ImmutableDictionary<string, CTestEnum> enumsByName,
        ImmutableDictionary<string, CTestRecord> recordsByName)
    {
        TargetPlatform = targetPlatform.ToString();
        _functionsByName = functionsByName;
        _enumsByName = enumsByName;
        _recordsByName = recordsByName;

        AssertPInvokePlatformNameFunction();
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

    public CTestRecord GetRecord(string name)
    {
        var exists = _recordsByName.TryGetValue(name, out var value);
        Assert.True(exists, $"The record `{name}` does not exist.");
        AssertRecord(value!);
        return value!;
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

        if (record.IsStruct)
        {
            var expectedSize = record.Fields.Sum(x => x.SizeOf + x.PaddingOf);
            Assert.True(
                expectedSize == record.SizeOf,
                $"C struct `{record.Name}` size does not match the total size of it's fields.");
        }
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
            field.PaddingOf >= 0,
            $"C {recordKindName} `{record.Name} field `{field.Name}` does not have an padding of which is positive or zero.");
        Assert.True(
            field.SizeOf > 0,
            $"C {recordKindName} `{record.Name} field `{field.Name}` does not have a size of which is positive.");

        if (record.IsUnion)
        {
            Assert.True(
                field.OffsetOf == 0,
                $"C union `{record.Name} field `{field.Name}` does not have an offset of zero.");
            Assert.True(
                field.PaddingOf == 0,
                $"C union `{record.Name} field `{field.Name}` does not have a padding of zero.");
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
