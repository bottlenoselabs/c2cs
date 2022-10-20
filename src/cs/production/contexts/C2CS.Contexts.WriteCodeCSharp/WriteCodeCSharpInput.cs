// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.WriteCodeCSharp.CodeGenerator;
using C2CS.Contexts.WriteCodeCSharp.Mapper;

namespace C2CS.Contexts.WriteCodeCSharp;

public sealed class WriteCodeCSharpInput
{
    public ImmutableArray<string> InputFilePaths { get; init; }

    public string OutputFilePath { get; init; } = string.Empty;

    public CSharpCodeMapperOptions MapperOptions { get; init; } = null!;

    public CSharpCodeGeneratorOptions GeneratorOptions { get; init; } = null!;
}
