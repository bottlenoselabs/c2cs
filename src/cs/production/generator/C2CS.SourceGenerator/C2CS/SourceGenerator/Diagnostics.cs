// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.CodeAnalysis;

namespace C2CS.SourceGenerator;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor BindgenProgramNotFound = new(
        id: "BINDGEN001",
        title: "Could not find bindgen program",
        messageFormat: "Could not find bindgen program. Do you have it installed? Check the documentation (https://github.com/bottlenoselabs/c2cs/tree/main/docs) for installation instructions.",
        category: "Bindgen",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BindgenClassNotFound = new(
        id: "BINDGEN002",
        title: "Could not find partial class target to generate bindings for",
        messageFormat: $"Could not find a partial class to generate bindings for. Please ensure you have a partial class 'partial class MyClassName' with the attribute '{typeof(BindgenAttribute).FullName}' attached.",
        category: "Bindgen",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BindgenFailed = new(
        id: "BINDGEN003",
        title: "Bindgen error",
        messageFormat: "Failed to generate bindings for partial class '{0}'. Configuration file path: '{1}'. Log: '{2}'.",
        category: "Bindgen",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BindgenNoSourceCode = new(
        id: "BINDGEN004",
        title: "Bindgen no output",
        messageFormat: "Bindgen program succeeded for partial class '{0}' but there is no C# source code output. Configuration file path: '{1}'. Log: '{2}'.",
        category: "Bindgen",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
