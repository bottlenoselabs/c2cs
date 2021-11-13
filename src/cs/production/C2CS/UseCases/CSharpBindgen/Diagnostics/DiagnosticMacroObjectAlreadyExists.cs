// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.UseCases.CSharpBindgen;

public class DiagnosticMacroObjectAlreadyExists : Diagnostic
{
    public DiagnosticMacroObjectAlreadyExists(string name, ClangLocation loc)
        : base(DiagnosticSeverity.Warning)
    {
        Summary = $"The object-like macro '{name}' at {loc.FilePath}:{loc.LineNumber}:{loc.LineColumn} already previously exists.";
    }
}
