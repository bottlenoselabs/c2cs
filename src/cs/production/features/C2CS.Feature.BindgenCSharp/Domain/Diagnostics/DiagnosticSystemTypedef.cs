// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;

namespace C2CS.Feature.BindgenCSharp.Domain.Diagnostics;

public sealed class DiagnosticSystemTypedef : Diagnostic
{
    public DiagnosticSystemTypedef(string typeName, CLocation location, string underlyingTypeName)
        : base(DiagnosticSeverity.Warning, CreateMessage(typeName, location, underlyingTypeName))
    {
    }

    private static string CreateMessage(string typeName, CLocation location, string underlyingTypeName)
    {
        return $"The typedef '{typeName}' at {location.FilePath}:{location.LineNumber}:{location.LineColumn} is a system alias to the system type '{underlyingTypeName}'. If you intend to have cross-platform bindings this is a problem; please create an issue on GitHub.";
    }
}
