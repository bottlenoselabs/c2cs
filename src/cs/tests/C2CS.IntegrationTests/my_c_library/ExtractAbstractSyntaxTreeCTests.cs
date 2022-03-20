// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ExtractAbstractSyntaxTreeC;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;
using JetBrains.Annotations;
using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1707
#pragma warning disable SA1300
#pragma warning disable IDE1006
#pragma warning disable CA1034

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class ExtractAbstractSyntaxTreeCTests : IClassFixture<ExtractAbstractSyntaxTreeCTests.Fixture>
{
    private readonly Fixture _fixture;

    public ExtractAbstractSyntaxTreeCTests(Fixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void function_void_void()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue(nameof(function_void_void), out var value));

        Assert.True(value!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(value.ReturnType == "void");
        Assert.True(value.Parameters.IsDefaultOrEmpty);
    }

    [Fact]
    public void function_void_string()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue(nameof(function_void_string), out var value));

        Assert.True(value!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(value.ReturnType == "void");

        Assert.True(!value.Parameters.IsDefaultOrEmpty);
        Assert.True(value.Parameters.Length == 1);

        var parameter = value.Parameters[0];
        Assert.True(parameter.Type == "char*");
    }

    [Fact]
    public void function_void_uint16_int32_uint64()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue(nameof(function_void_uint16_int32_uint64), out var value));

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

    [Fact]
    public void function_void_uint16ptr_int32ptr_uint64ptr()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue(nameof(function_void_uint16ptr_int32ptr_uint64ptr), out var value));

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

    [Fact]
    public void function_void_enum()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue(nameof(function_void_enum), out var value));

        Assert.True(value!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(value.ReturnType == "void");

        Assert.True(!value.Parameters.IsDefaultOrEmpty);
        Assert.True(value.Parameters.Length == 1);

        var firstParameter = value.Parameters[0];
        Assert.True(firstParameter.Type == "enum_force_uint32");
    }

    [Fact]
    public void enum_force_uint32()
    {
        Assert.True(_fixture.EnumsByName.TryGetValue(nameof(enum_force_uint32), out var value));
        Assert.True(value!.IntegerType == "signed int");
        Assert.True(!value.Values.IsDefaultOrEmpty);
    }

    [UsedImplicitly]
    public sealed class Fixture
    {
        public readonly ImmutableDictionary<string, CFunction> FunctionsByName;
        public readonly ImmutableDictionary<string, CEnum> EnumsByName;

        public Fixture()
        {
            var configuration = new ConfigurationExtractAbstractSyntaxTreeC
            {
                IsEnabledFindSdk = true,
                InputFilePath = "my_c_library/include/my_c_library.h"
            };

            var request = new Input(configuration);
            var useCase = new Handler();
            var response = useCase.Execute(request);

            Assert.True(response.Status == UseCaseOutputStatus.Success);
            Assert.True(response.Diagnostics.Length == 0);

            var ast = response.AbstractSyntaxTree;
            Assert.True(ast != null);

            FunctionsByName = ast!.Functions.ToImmutableDictionary(x => x.Name, x => x);
            EnumsByName = ast.Enums.ToImmutableDictionary(x => x.Name, x => x);

            Assert.True(FunctionsByName.TryGetValue("c2cs_get_runtime_platform_name", out var function));
            Assert.True(function!.CallingConvention == CFunctionCallingConvention.Cdecl);
            Assert.True(function.ReturnType == "char*");
            Assert.True(function.Parameters.IsDefaultOrEmpty);
        }
    }
}
