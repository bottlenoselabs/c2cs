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
        : base(TestHost.Services, "my_c_library", "Data/CSharp", true)
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
        var value = _context.GetFunction(name);
        AssertValue(name, value, "Functions");
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
