// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.IntegrationTests.my_c_library.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class BindgenCSharpTests : IntegrationTest
{
    private readonly BindgenCSharpFixture _fixture;

    public BindgenCSharpTests()
    {
        _fixture = Services.GetService<BindgenCSharpFixture>()!;
    }

    [Fact]
    public void function_void_void()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue(nameof(function_void_void), out var value));

        Assert.True(value!.ReturnType.ToString() == "void");
        Assert.True(value.ParameterList.Parameters.Count == 0);
    }

    [Fact]
    public void function_void_string()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue(nameof(function_void_string), out var value));

        Assert.True(value!.ReturnType.ToString() == "void");

        Assert.True(value.ParameterList.Parameters.Count == 1);

        var parameter = value.ParameterList.Parameters[0];
        Assert.True(parameter.Type!.ToString() == "CString");
    }

    [Fact]
    public void function_void_uint16_int32_uint64()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue(nameof(function_void_uint16_int32_uint64), out var value));

        Assert.True(value!.ReturnType.ToString() == "void");

        Assert.True(value.ParameterList.Parameters.Count == 3);

        var firstParameter = value.ParameterList.Parameters[0];
        Assert.True(firstParameter.Type!.ToString() == "ushort");

        var secondParameter = value.ParameterList.Parameters[1];
        Assert.True(secondParameter.Type!.ToString() == "int");

        var thirdParameter = value.ParameterList.Parameters[2];
        Assert.True(thirdParameter.Type!.ToString() == "ulong");
    }

    [Fact]
    public void function_void_uint16ptr_int32ptr_uint64ptr()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue(nameof(function_void_uint16ptr_int32ptr_uint64ptr), out var value));

        Assert.True(value!.ReturnType.ToString() == "void");

        Assert.True(value.ParameterList.Parameters.Count == 3);

        var firstParameter = value.ParameterList.Parameters[0];
        Assert.True(firstParameter.Type!.ToString() == "ushort*");

        var secondParameter = value.ParameterList.Parameters[1];
        Assert.True(secondParameter.Type!.ToString() == "int*");

        var thirdParameter = value.ParameterList.Parameters[2];
        Assert.True(thirdParameter.Type!.ToString() == "ulong*");
    }

    [Fact]
    public void function_void_enum()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue(nameof(function_void_enum), out var value));

        Assert.True(value!.ReturnType.ToString() == "void");

        Assert.True(value.ParameterList.Parameters.Count == 1);

        var firstParameter = value.ParameterList.Parameters[0];
        Assert.True(firstParameter.Type!.ToString() == "enum_force_uint32");
    }

    [Fact]
    public void enum_force_uint32()
    {
        Assert.True(_fixture.EnumsByName.TryGetValue(nameof(enum_force_uint32), out var value));
        Assert.True(value!.BaseList!.Types[0].Type.ToString() == "int");
        Assert.True(value.Members.Count > 0);
    }
}
