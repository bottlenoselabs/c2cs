// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using NuGet.Frameworks;

namespace C2CS.Commands.WriteCodeCSharp.Domain.CodeGenerator;

public sealed class CSharpCodeGeneratorOptions
{
    public NuGetFramework TargetFramework { get; set; } = NuGetFramework.AgnosticFramework;

    public string ClassName { get; init; } = string.Empty;

    public string LibraryName { get; init; } = string.Empty;

    public string NamespaceName { get; init; } = string.Empty;

    public string HeaderCodeRegion { get; init; } = string.Empty;

    public string FooterCodeRegion { get; init; } = string.Empty;

    public bool IsEnabledGenerateCSharpRuntimeCode { get; init; }

    public bool IsEnabledFunctionPointers { get; init; }

    public bool IsEnabledRuntimeMarshalling { get; init; }

    public bool IsEnabledLibraryImportAttribute { get; init; }

    public bool IsEnabledFileScopedNamespace { get; init; }
}
