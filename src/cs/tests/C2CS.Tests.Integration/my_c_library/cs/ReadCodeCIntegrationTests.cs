// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using C2CS.Feature.ReadCodeC.Data.Model;
using C2CS.IntegrationTests.my_c_library.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class ReadCodeCIntegrationTests : IntegrationTest
{
    private readonly ReadCodeCFixture _fixture;

    public ReadCodeCIntegrationTests()
    {
        _fixture = Services.GetService<ReadCodeCFixture>()!;
        _fixture.AssertPlatform();
    }

    [Fact]
    public void enum_force_uint32()
    {
        Assert.True(_fixture.AbstractSyntaxTrees.Length > 0);
        foreach (var ast in _fixture.AbstractSyntaxTrees)
        {
            Assert.True(ast.EnumsByName.TryGetValue(nameof(enum_force_uint32), out var value));
            Assert.True(value!.IntegerType == "unsigned int" || value.IntegerType == "int");
            Assert.True(!value.Values.IsDefaultOrEmpty);
        }
    }

    [Fact]
    public void function_void_void()
    {
        Assert.True(_fixture.AbstractSyntaxTrees.Length > 0);
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
        Assert.True(_fixture.AbstractSyntaxTrees.Length > 0);
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
        Assert.True(_fixture.AbstractSyntaxTrees.Length > 0);
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
        Assert.True(_fixture.AbstractSyntaxTrees.Length > 0);
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
        Assert.True(_fixture.AbstractSyntaxTrees.Length > 0);
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
    public void function_void_struct_union()
    {
        Assert.True(_fixture.AbstractSyntaxTrees.Length > 0);
        foreach (var ast in _fixture.AbstractSyntaxTrees)
        {
            Assert.True(ast.FunctionsByName.TryGetValue(nameof(function_void_struct_union), out var value));

            Assert.True(value!.CallingConvention == CFunctionCallingConvention.Cdecl);
            Assert.True(value.ReturnType == "void");

            Assert.True(!value.Parameters.IsDefaultOrEmpty);
            Assert.True(value.Parameters.Length == 1);

            var firstParameter = value.Parameters[0];
            Assert.True(firstParameter.Type == "struct_union");

            Assert.True(ast.StructsByName.TryGetValue("struct_union", out var structType));
            Console.WriteLine(structType);
        }
    }
}
