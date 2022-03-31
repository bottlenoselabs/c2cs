// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ReadCodeC.Data;
using C2CS.IntegrationTests.my_c_library.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class ExtractAbstractSyntaxTreeCIntegrationTests : IntegrationTest
{
    private readonly ExtractAbstractSyntaxTreeCFixture _fixture;

    public ExtractAbstractSyntaxTreeCIntegrationTests()
    {
        _fixture = Services.GetService<ExtractAbstractSyntaxTreeCFixture>()!;
    }

    [Fact]
    public void function_void_void()
    {
        foreach (var ast in _fixture.AbstractSyntaxTrees)
        {
            Assert.True(ast.FunctionsByName.TryGetValue(nameof(function_void_void), out var value));

            Assert.True(value!.CallingConvention == CFunctionCallingConvention.Cdecl);
            Assert.True(value.ReturnType == "void");
            Assert.True(value.Parameters.IsDefaultOrEmpty);
        }
    }

    [Fact]
    public void function_void_string()
    {
        foreach (var ast in _fixture.AbstractSyntaxTrees)
        {
            Assert.True(ast.FunctionsByName.TryGetValue(nameof(function_void_string), out var value));

            Assert.True(value!.CallingConvention == CFunctionCallingConvention.Cdecl);
            Assert.True(value.ReturnType == "void");

            Assert.True(!value.Parameters.IsDefaultOrEmpty);
            Assert.True(value.Parameters.Length == 1);

            var parameter = value.Parameters[0];
            Assert.True(parameter.Type == "char*");
        }
    }

    [Fact]
    public void function_void_uint16_int32_uint64()
    {
        foreach (var ast in _fixture.AbstractSyntaxTrees)
        {
            Assert.True(ast.FunctionsByName.TryGetValue(nameof(function_void_uint16_int32_uint64), out var value));

            Assert.True(value!.CallingConvention == CFunctionCallingConvention.Cdecl);
            Assert.True(value.ReturnType == "void");

            Assert.True(!value.Parameters.IsDefaultOrEmpty);
            Assert.True(value.Parameters.Length == 3);

            var firstParameter = value.Parameters[0];
            Assert.True(firstParameter.Type == "uint16_t");

            var secondParameter = value.Parameters[1];
            Assert.True(secondParameter.Type == "int32_t");

            var thirdParameter = value.Parameters[2];
            Assert.True(thirdParameter.Type == "uint64_t");
        }
    }

    [Fact]
    public void function_void_uint16ptr_int32ptr_uint64ptr()
    {
        foreach (var ast in _fixture.AbstractSyntaxTrees)
        {
            Assert.True(ast.FunctionsByName.TryGetValue(
                nameof(function_void_uint16ptr_int32ptr_uint64ptr),
                out var value));

            Assert.True(value!.CallingConvention == CFunctionCallingConvention.Cdecl);
            Assert.True(value.ReturnType == "void");

            Assert.True(!value.Parameters.IsDefaultOrEmpty);
            Assert.True(value.Parameters.Length == 3);

            var firstParameter = value.Parameters[0];
            Assert.True(firstParameter.Type == "uint16_t*");

            var secondParameter = value.Parameters[1];
            Assert.True(secondParameter.Type == "int32_t*");

            var thirdParameter = value.Parameters[2];
            Assert.True(thirdParameter.Type == "uint64_t*");
        }
    }

    [Fact]
    public void function_void_enum()
    {
        foreach (var ast in _fixture.AbstractSyntaxTrees)
        {
            Assert.True(ast.FunctionsByName.TryGetValue(nameof(function_void_enum), out var value));

            Assert.True(value!.CallingConvention == CFunctionCallingConvention.Cdecl);
            Assert.True(value.ReturnType == "void");

            Assert.True(!value.Parameters.IsDefaultOrEmpty);
            Assert.True(value.Parameters.Length == 1);

            var firstParameter = value.Parameters[0];
            Assert.True(firstParameter.Type == "enum_force_uint32");
        }
    }

    [Fact]
    public void enum_force_uint32()
    {
        foreach (var ast in _fixture.AbstractSyntaxTrees)
        {
            Assert.True(ast.EnumsByName.TryGetValue(nameof(enum_force_uint32), out var value));
            Assert.True(value!.IntegerType == "signed int");
            Assert.True(!value.Values.IsDefaultOrEmpty);
        }
    }
}
