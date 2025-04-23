// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using NuGet.Frameworks;

namespace C2CS.GenerateCSharpCode;

public class InputSanitized
{
    public string InputFilePath { get; init; } = string.Empty;

    public string OutputFileDirectory { get; init; } = string.Empty;

    public NuGetFramework TargetFramework { get; set; } = NuGetFramework.AgnosticFramework;

    public string ClassName { get; init; } = string.Empty;

    public string LibraryName { get; init; } = string.Empty;

    public string NamespaceName { get; init; } = string.Empty;

    public bool IsEnabledAccessModifierInternal { get; init; } = true;

    public string CodeRegionHeader { get; init; } = string.Empty;

    public string CodeRegionFooter { get; init; } = string.Empty;

    public ImmutableDictionary<string, string> MappedNames { get; init; } = ImmutableDictionary<string, string>.Empty;

    public bool IsEnabledGenerateCSharpRuntimeCode { get; init; }

    public bool IsEnabledFunctionPointers { get; init; }

    public bool IsEnabledRuntimeMarshalling { get; init; }

    public bool IsEnabledLibraryImportAttribute { get; init; }

    public bool IsEnabledFileScopedNamespace { get; init; }

    public bool IsEnabledSpans { get; init; }
}
