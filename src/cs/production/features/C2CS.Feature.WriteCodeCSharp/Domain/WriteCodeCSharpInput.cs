// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.WriteCodeCSharp.Data;

namespace C2CS.Feature.WriteCodeCSharp.Domain;

public sealed class WriteCodeCSharpInput
{
    public ImmutableArray<string> InputFilePaths { get; init; }

    public string OutputFilePath { get; init; } = string.Empty;

    public ImmutableArray<CSharpTypeAlias> TypeAliases { get; init; }

    public ImmutableArray<string> IgnoredNames { get; init; }

    public string LibraryName { get; init; } = string.Empty;

    public string ClassName { get; init; } = string.Empty;

    public string NamespaceName { get; init; } = string.Empty;

    public string HeaderCodeRegion { get; init; } = string.Empty;

    public string FooterCodeRegion { get; init; } = string.Empty;
}
