// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.WriteCodeCSharp.Data;
using C2CS.Contexts.WriteCodeCSharp.Data.Model;

namespace C2CS.Contexts.WriteCodeCSharp.Domain;

public sealed class WriteCodeCSharpInput
{
    public ImmutableArray<string> InputFilePaths { get; set; }

    public string OutputFilePath { get; set; } = string.Empty;

    public ImmutableArray<CSharpTypeAlias> TypeAliases { get; set; }

    public ImmutableArray<string> IgnoredNames { get; set; }

    public string LibraryName { get; set; } = string.Empty;

    public string ClassName { get; set; } = string.Empty;

    public string NamespaceName { get; set; } = string.Empty;

    public string HeaderCodeRegion { get; set; } = string.Empty;

    public string FooterCodeRegion { get; set; } = string.Empty;
}
