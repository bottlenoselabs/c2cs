// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.BindgenCSharp.Diagnostics;

public class SystemTypedefDiagnostic : Diagnostic
{
    public SystemTypedefDiagnostic(string typeName, ClangLocation loc, string underlyingTypeName)
        : base(DiagnosticSeverity.Warning)
    {
        Summary =
            $"The typedef '{typeName}' at {loc.FilePath}:{loc.LineNumber}:{loc.LineColumn} is a system alias to the system type '{underlyingTypeName}'. If you intend to have cross-platform bindings this is a problem; please create an issue on GitHub.";
    }
}
