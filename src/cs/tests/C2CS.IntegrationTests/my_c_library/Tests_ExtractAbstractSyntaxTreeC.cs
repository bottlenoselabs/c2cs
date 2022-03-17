// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.ComponentModel;
using C2CS.Feature.ExtractAbstractSyntaxTreeC;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;
using JetBrains.Annotations;
using Xunit;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1707
#pragma warning disable SA1300
#pragma warning disable IDE1006
#pragma warning disable CA1034

namespace C2CS.IntegrationTests;

[Trait("Integration", "my_c_library")]
public class Tests_ExtractAbstractSyntaxTreeC : IClassFixture<Tests_ExtractAbstractSyntaxTreeC.Fixture>
{
    private readonly Fixture _fixture;

    public Tests_ExtractAbstractSyntaxTreeC(Fixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void function_void_void()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue("function_void_void", out var function));

        Assert.True(function!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(function.ReturnType == "void");
        Assert.True(function.Parameters.IsDefaultOrEmpty);
    }

    [Fact]
    public void function_void_string()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue("function_void_string", out var function));

        Assert.True(function!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(function.ReturnType == "void");

        Assert.True(!function.Parameters.IsDefaultOrEmpty);
        Assert.True(function.Parameters.Length == 1);

        var parameter = function.Parameters[0];
        Assert.True(parameter.Type == "char*");
    }

    [Fact]
    public void function_void_uint16_int32_uint64()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue("function_void_uint16_int32_uint64", out var function));

        Assert.True(function!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(function.ReturnType == "void");

        Assert.True(!function.Parameters.IsDefaultOrEmpty);
        Assert.True(function.Parameters.Length == 3);

        var firstParameter = function.Parameters[0];
        Assert.True(firstParameter.Type == "uint16_t");

        var secondParameter = function.Parameters[1];
        Assert.True(secondParameter.Type == "int32_t");

        var thirdParameter = function.Parameters[2];
        Assert.True(thirdParameter.Type == "uint64_t");
    }

    [Fact]
    public void function_void_uint16ptr_int32ptr_uint64ptr()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue("function_void_uint16ptr_int32ptr_uint64ptr", out var function));

        Assert.True(function!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(function.ReturnType == "void");

        Assert.True(!function.Parameters.IsDefaultOrEmpty);
        Assert.True(function.Parameters.Length == 3);

        var firstParameter = function.Parameters[0];
        Assert.True(firstParameter.Type == "uint16_t*");

        var secondParameter = function.Parameters[1];
        Assert.True(secondParameter.Type == "int32_t*");

        var thirdParameter = function.Parameters[2];
        Assert.True(thirdParameter.Type == "uint64_t*");
    }

    [Fact]
    public void function_void_enum()
    {
        Assert.True(_fixture.FunctionsByName.TryGetValue("function_void_enum", out var function));

        Assert.True(function!.CallingConvention == CFunctionCallingConvention.Cdecl);
        Assert.True(function.ReturnType == "void");

        Assert.True(!function.Parameters.IsDefaultOrEmpty);
        Assert.True(function.Parameters.Length == 1);

        var firstParameter = function.Parameters[0];
        Assert.True(firstParameter.Type == "enum_force_uint32");
    }

    [UsedImplicitly]
    public sealed class Fixture
    {
        public readonly ImmutableDictionary<string, CFunction> FunctionsByName;

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
            Assert.True(response.AbstractSyntaxTree != null);

            FunctionsByName = response.AbstractSyntaxTree!.Functions
                .ToImmutableDictionary(x => x.Name, x => x);
        }
    }
}
