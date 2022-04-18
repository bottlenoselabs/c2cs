// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.IntegrationTests.my_c_library.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class WriteCodeCSharpIntegrationTests : IntegrationTest
{
    private readonly WriteCodeCSharpFixture _fixture;

    public WriteCodeCSharpIntegrationTests()
    {
        _fixture = Services.GetService<WriteCodeCSharpFixture>()!;
    }

    [Fact]
    public void enum_force_uint32()
    {
        const string name = nameof(enum_force_uint32);
        var value = _fixture.GetEnum(name);

        var baseTypeName = value.BaseList!.Types[0].Type.ToString();
        Assert.True(baseTypeName == "int", $"Enum `{name}` does not have a base type of `int`.");

        var membersCount = value.Members.Count;
        Assert.True(membersCount > 0, $"Enum `{name}` has no members.");
    }

    [Fact]
    public void function_void_void()
    {
        const string name = nameof(function_void_void);
        var value = _fixture.GetFunction(name);

        var returnTypeName = value.ReturnType.ToString();
        Assert.True(returnTypeName == "void", $"The function `{name}` does not have a return type of `void`.");

        var parametersCount = value.ParameterList.Parameters.Count;
        Assert.True(parametersCount == 0, $"The function `{name}` does not have 0 parameters.");
    }

    [Fact]
    public void function_void_string()
    {
        const string name = nameof(function_void_string);
        var value = _fixture.GetFunction(name);

        var returnTypeName = value.ReturnType.ToString();
        Assert.True(returnTypeName == "void", $"The function `{name}` does not have a return type of `void`.");

        Assert.True(value.ParameterList.Parameters.Count == 1, $"The function `{name}` does not have 1 parameter(s).");

        var firstParameter = value.ParameterList.Parameters[0];
        var firstParameterTypeName = firstParameter.Type?.ToString() ?? string.Empty;
        Assert.True(firstParameterTypeName == "CString", $"The function `{name}` does not have a parameter at index 0 with the type `CString`.");
    }

    [Fact]
    public void function_void_uint16_int32_uint64()
    {
        const string name = nameof(function_void_uint16_int32_uint64);
        var value = _fixture.GetFunction(name);

        var returnTypeName = value.ReturnType.ToString();
        Assert.True(returnTypeName == "void", $"The function `{name}` does not have a base type of `void`.");

        var parametersCount = value.ParameterList.Parameters.Count;
        Assert.True(parametersCount == 3, $"The function `{name}` does not have 3 parameter(s).");

        var firstParameter = value.ParameterList.Parameters[0];
        var firstParameterTypeName = firstParameter.Type?.ToString() ?? string.Empty;
        Assert.True(firstParameterTypeName == "ushort", $"The function `{name}` does not have a parameter at index 0 with the type `ushort`.");

        var secondParameter = value.ParameterList.Parameters[1];
        var secondParameterTypeName = secondParameter.Type?.ToString() ?? string.Empty;
        Assert.True(secondParameterTypeName == "int", $"The function `{name}` does not have a parameter at index 1 with the type `int`.");

        var thirdParameter = value.ParameterList.Parameters[2];
        var thirdParameterTypeName = thirdParameter.Type?.ToString() ?? string.Empty;
        Assert.True(thirdParameterTypeName == "ulong", $"The function `{name}` does not have a parameter at index 2 with the type `ulong`.");
    }

    [Fact]
    public void function_void_uint16ptr_int32ptr_uint64ptr()
    {
        const string name = nameof(function_void_uint16ptr_int32ptr_uint64ptr);
        var value = _fixture.GetFunction(name);

        var returnTypeName = value.ReturnType.ToString();
        Assert.True(returnTypeName == "void", $"The function `{name}` does not have a base type of `void`.");

        var parametersCount = value.ParameterList.Parameters.Count;
        Assert.True(parametersCount == 3, $"The function `{name}` does not have 3 parameter(s).");

        var firstParameter = value.ParameterList.Parameters[0];
        var firstParameterTypeName = firstParameter.Type?.ToString() ?? string.Empty;
        Assert.True(firstParameterTypeName == "ushort*", $"The function `{name}` does not have a parameter at index 0 with the type `ushort*`.");

        var secondParameter = value.ParameterList.Parameters[1];
        var secondParameterTypeName = secondParameter.Type?.ToString() ?? string.Empty;
        Assert.True(secondParameterTypeName == "int*", $"The function `{name}` does not have a parameter at index 1 with the type `int*`.");

        var thirdParameter = value.ParameterList.Parameters[2];
        var thirdParameterTypeName = thirdParameter.Type?.ToString() ?? string.Empty;
        Assert.True(thirdParameterTypeName == "ulong*", $"The function `{name}` does not have a parameter at index 2 with the type `ulong*`.");
    }

    [Fact]
    public void function_void_enum()
    {
        const string name = nameof(function_void_enum);
        var value = _fixture.GetFunction(name);

        var returnTypeName = value.ReturnType.ToString();
        Assert.True(returnTypeName == "void", $"The function `{name}` does not have a base type of `void`.");

        var parametersCount = value.ParameterList.Parameters.Count;
        Assert.True(parametersCount == 1, $"The function `{name}` does not have 1 parameter(s).");

        var firstParameter = value.ParameterList.Parameters[0];
        var firstParameterTypeName = firstParameter.Type?.ToString() ?? string.Empty;
        Assert.True(firstParameterTypeName == "enum_force_uint32", $"The function `{name}` does not have a parameter at index 0 with the type `enum_force_uint32`.");
    }

    [Fact]
    public void function_void_struct_union()
    {
        const string name = nameof(function_void_struct_union);
        var value = _fixture.GetFunction(name);

        var returnTypeName = value.ReturnType.ToString();
        Assert.True(returnTypeName == "void", $"The function `{name}` does not have a base type of `void`.");

        var parametersCount = value.ParameterList.Parameters.Count;
        Assert.True(parametersCount == 1, $"The function `{name}` does not have 1 parameter(s).");

        var firstParameter = value.ParameterList.Parameters[0];
        var firstParameterTypeName = firstParameter.Type?.ToString() ?? string.Empty;
        Assert.True(firstParameterTypeName == "struct_union", $"The function `{name}` does not have a parameter at index 0 with the type `struct_union`.");
    }
}
