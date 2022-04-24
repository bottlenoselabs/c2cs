// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.IntegrationTests.my_c_library.Fixtures;
using C2CS.Tests.Common;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class WriteCodeCSharpTests : CLibraryIntegrationTest
{
    private readonly WriteCodeCSharpFixtureContext _context;

    public WriteCodeCSharpTests()
        : base(TestHost.Services, "my_c_library", "Data/CSharp", false)
    {
        _context = TestHost.Services.GetService<WriteCodeCSharpFixture>()!.Context;
    }

    [Theory]
    [InlineData("enum_force_uint32")]
    public void Enum(string name)
    {
        var value = _context.GetEnum(name);
        AssertValue(name, value, "Enums");
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
        var value = _context.GetFunction(name);
        AssertValue(name, value, "Functions");
    }

    [Theory]
    [InlineData("struct_union_anonymous")]
    [InlineData("struct_union_anonymous_with_field_name")]
    [InlineData("struct_union_named")]
    [InlineData("struct_leaf_integers_small_to_large")]
    [InlineData("struct_leaf_integers_large_to_small")]
    public void Struct(string name)
    {
        var value = _context.GetStruct(name);
        AssertValue(name, value, "Structs");
    }

    [Fact]
    public void Compiles()
    {
        var emitResult = _context.EmitResult;
        Assert.True(emitResult.Success, "C# code did not compile successfully.");

        foreach (var diagnostic in emitResult.Diagnostics)
        {
            var isWarningOrError = diagnostic.Severity != DiagnosticSeverity.Warning &&
                                   diagnostic.Severity != DiagnosticSeverity.Error;
            Assert.True(isWarningOrError, $"C# code compilation diagnostic: {diagnostic}.");
        }
    }
}
