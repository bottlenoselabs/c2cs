// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.IntegrationTests.c_library.Fixtures;
using C2CS.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.IntegrationTests.c_library;

[Trait("Integration", "c_library")]
public class ReadCodeC : CLibraryIntegrationTest
{
    private readonly ImmutableArray<ReadCodeCFixtureContext> _contexts;

    public ReadCodeC()
        : base(TestHost.Services, "c_library", "Data/C", false)
    {
        _contexts = TestHost.Services.GetService<ReadCodeCFixture>()!.Contexts;
        Assert.True(_contexts.Length > 0, "Failed to read C code.");

        foreach (var context in _contexts)
        {
            if (!context.ParseOptions.IsEnabledSingleHeader)
            {
                var functionIgnored = context.TryGetFunction("function_ignored");
                Assert.True(functionIgnored == null);
            }
        }
    }

    [Theory]
    [InlineData("enum_force_uint32")]
    public void Enum(string name)
    {
        foreach (var context in _contexts)
        {
            var value = context.GetEnum(name);
            AssertValue(name, value, $"{context.TargetPlatformRequested}/Enums");
        }
    }

    [Theory]
    [InlineData("function_void_void")]
    [InlineData("function_void_intptr")]
    [InlineData("function_void_intptr_1")]
    [InlineData("function_void_intptr_2")]
    [InlineData("function_void_intptr_3")]
    [InlineData("function_void_intptr_4")]
    [InlineData("function_void_intptr_5")]
    [InlineData("function_void_intptr_6")]
    [InlineData("function_void_intptr_7")]
    [InlineData("function_void_intptr_8")]
    [InlineData("function_void_intptr_9")]
    [InlineData("function_void_intptr_10")]
    [InlineData("function_void_intptr_11")]
    [InlineData("function_void_intptr_12")]
    [InlineData("function_void_intptr_13")]
    [InlineData("function_void_intptr_14")]
    [InlineData("function_void_intptr_15")]
    [InlineData("function_void_intptr_16")]
    [InlineData("function_void_intptr_17")]
    [InlineData("function_void_intptr_18")]
    [InlineData("function_void_intptr_19")]
    [InlineData("function_void_intptr_20")]
    [InlineData("function_void_intptr_21")]
    [InlineData("function_void_intptr_22")]
    [InlineData("function_void_intptr_23")]
    [InlineData("function_void_intptr_24")]
    [InlineData("function_void_intptr_25")]
    [InlineData("function_void_intptr_26")]
    [InlineData("function_void_intptr_27")]
    [InlineData("function_void_intptr_28")]
    [InlineData("function_void_string")]
    [InlineData("function_void_uint16_int32_uint64")]
    [InlineData("function_void_uint16ptr_int32ptr_uint64ptr")]
    [InlineData("function_void_enum")]
    [InlineData("function_void_struct_union_anonymous")]
    [InlineData("function_void_struct_union_anonymous_with_field_name")]
    [InlineData("function_void_struct_union_named")]
    public void Function(string name)
    {
        foreach (var context in _contexts)
        {
            var value = context.GetFunction(name);
            AssertValue(name, value, $"{context.TargetPlatformRequested}/Functions");
        }
    }

    [Theory]
    [InlineData("struct_union_anonymous")]
    [InlineData("struct_union_anonymous_with_field_name")]
    [InlineData("struct_union_named")]
    [InlineData("struct_leaf_integers_small_to_large")]
    [InlineData("struct_leaf_integers_large_to_small")]
    [InlineData("struct_bitfield_one_fields_1")]
    [InlineData("struct_bitfield_one_fields_2")]
    [InlineData("struct_bitfield_one_fields_3")]
    public void Struct(string name)
    {
        foreach (var context in _contexts)
        {
            var value = context.GetRecord(name);
            AssertValue(name, value, $"{context.TargetPlatformRequested}/Structs");
        }
    }

    [Theory]
    [InlineData("MY_CONSTANT")]
    public void MacroObject(string name)
    {
        foreach (var context in _contexts)
        {
            var value = context.GetMacroObject(name);
            AssertValue(name, value, $"{context.TargetPlatformRequested}/MacroObjects");
        }
    }
}
