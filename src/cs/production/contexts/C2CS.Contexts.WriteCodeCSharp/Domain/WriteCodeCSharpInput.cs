// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.WriteCodeCSharp.Data;
using C2CS.Contexts.WriteCodeCSharp.Data.Model;
using C2CS.Contexts.WriteCodeCSharp.Domain.CodeGenerator;

namespace C2CS.Contexts.WriteCodeCSharp.Domain;

public sealed class WriteCodeCSharpInput
{
    public ImmutableArray<string> InputFilePaths { get; init; }

    public string OutputFilePath { get; init; } = string.Empty;

    public ImmutableArray<CSharpTypeAlias> TypeAliases { get; init; }

    public ImmutableArray<string> IgnoredNames { get; init; }

    public CSharpCodeGeneratorOptions Options { get; init; } = null!;
}
