// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.IO;
using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using C2CS.IntegrationTests.my_c_library.Fixtures;
using C2CS.Tests.Common;
using C2CS.Tests.Common.Data.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace C2CS.IntegrationTests.my_c_library;

[Trait("Integration", "my_c_library")]
public class WriteCodeCSharpTests : CLibraryIntegrationTest
{
    private readonly bool _regenerateDataFiles;
    private readonly IFileSystem _fileSystem;
    private readonly WriteCodeCSharpFixture _fixture;
    private readonly CSharpGeneratedJsonSerializer _serializer;

    public WriteCodeCSharpTests()
        : base("my_c_library")
    {
        // Change to `true` to regenerate test files from actual data, then turn to `false` after to use test files.
        _regenerateDataFiles = true;
        _fileSystem = TestHost.Services.GetService<IFileSystem>()!;
        _fixture = TestHost.Services.GetService<WriteCodeCSharpFixture>()!;
        _serializer = TestHost.Services.GetService<CSharpGeneratedJsonSerializer>()!;
    }

    [Fact]
    public void enum_force_uint32()
    {
        AssertEnum(nameof(enum_force_uint32));
    }

    [Fact]
    public void function_void_void()
    {
        AssertFunction(nameof(function_void_void));
    }

    [Fact]
    public void function_void_string()
    {
        AssertFunction(nameof(function_void_string));
    }

    [Fact]
    public void function_void_uint16_int32_uint64()
    {
        AssertFunction(nameof(function_void_uint16_int32_uint64));
    }

    [Fact]
    public void function_void_uint16ptr_int32ptr_uint64ptr()
    {
        AssertFunction(nameof(function_void_uint16ptr_int32ptr_uint64ptr));
    }

    [Fact]
    public void function_void_enum()
    {
        AssertFunction(nameof(function_void_enum));
    }

    [Fact]
    public void function_void_struct_union_anonymous()
    {
        AssertFunction(nameof(function_void_struct_union_anonymous));
    }

    [Fact]
    public void function_void_struct_union_named()
    {
        AssertFunction(nameof(function_void_struct_union_named));
    }

    private void AssertFunction(string name)
    {
        var value = _fixture.GetFunction(name);
        AssertValue(name, value);
    }

    private void AssertEnum(string name)
    {
        var value = _fixture.GetEnum(name);
        AssertValue(name, value);
    }

    private void AssertValue<T>(string name, T value)
    {
        if (_regenerateDataFiles)
        {
            RegenerateDataFile(name, value);
        }

        var jsonActual = _serializer.WriteToString(value);
        var jsonExpected = ReadTestFileContents(name);
        Assert.Equal(jsonExpected, jsonActual);
    }

    private void RegenerateDataFile<T>(string name, T value, [CallerFilePath] string? sourceCodeFilePath = null)
    {
        var directory = _fileSystem.Path.GetDirectoryName(sourceCodeFilePath);
        var jsonFilePath = _fileSystem.Path.Combine(directory, "Data", $"{name}.json");
        _serializer.WriteToFile(jsonFilePath, value);
    }
}
