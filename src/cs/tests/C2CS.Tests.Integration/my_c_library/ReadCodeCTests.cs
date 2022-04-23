// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.IntegrationTests.my_c_library.Fixtures;
using C2CS.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class ReadCodeCTests : CLibraryIntegrationTest
{
    private readonly ImmutableArray<ReadCodeCFixtureContext> _contexts;

    public ReadCodeCTests()
        : base(TestHost.Services, "my_c_library", "Data/C", true)
    {
        _contexts = TestHost.Services.GetService<ReadCodeCFixture>()!.Contexts;
        Assert.True(_contexts.Length > 0, "Failed to read C code.");
    }

    [Theory]
    [InlineData("enum_force_uint32")]
    public void Enum(string name)
    {
        foreach (var context in _contexts)
        {
            var value = context.GetEnum(name);
            AssertValue(name, value, $"{context.TargetPlatform}/Enums");
        }
    }

    [Theory]
    [InlineData("function_void_void")]
    [InlineData("function_void_string")]
    [InlineData("function_void_uint16_int32_uint64")]
    [InlineData("function_void_uint16ptr_int32ptr_uint64ptr")]
    [InlineData("function_void_enum")]
    [InlineData("function_void_struct_union_anonymous")]
    [InlineData("function_void_struct_union_anonymous_with_field_name")]
    [InlineData("function_void_struct_union_named")]
    [InlineData("function_void_struct_union_named_empty")]
    public void Function(string name)
    {
        foreach (var context in _contexts)
        {
            var value = context.GetFunction(name);
            AssertValue(name, value, $"{context.TargetPlatform}/Functions");
        }
    }

    [Theory]
    [InlineData("struct_union_anonymous")]
    [InlineData("struct_union_anonymous_with_field_name")]
    [InlineData("struct_union_named")]
    [InlineData("struct_union_named_empty")]
    [InlineData("struct_leaf_integers_small_to_large")]
    [InlineData("struct_leaf_integers_large_to_small")]
    public void Struct(string name)
    {
        foreach (var context in _contexts)
        {
            var value = context.GetRecord(name);
            AssertValue(name, value, $"{context.TargetPlatform}/Structs");
        }
    }
}
