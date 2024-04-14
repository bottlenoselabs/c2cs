// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator;
using C2CS.Commands.WriteCodeCSharp.Domain.Mapper;

namespace C2CS.Commands.WriteCodeCSharp.Input.Sanitized;

public sealed class WriteCodeCSharpInput
{
    public string InputFilePath { get; init; } = string.Empty;

    public string OutputFileDirectory { get; init; } = string.Empty;

    public CSharpCodeMapperOptions MapperOptions { get; init; } = null!;

    public CSharpCodeGeneratorOptions GeneratorOptions { get; init; } = null!;
}
