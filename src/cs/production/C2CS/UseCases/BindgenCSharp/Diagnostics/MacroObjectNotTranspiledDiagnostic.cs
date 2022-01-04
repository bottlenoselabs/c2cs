// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.UseCases.BindgenCSharp;

public class MacroObjectNotTranspiledDiagnostic : Diagnostic
{
    public MacroObjectNotTranspiledDiagnostic(string name, ClangLocation loc)
        : base(DiagnosticSeverity.Warning)
    {
        Summary =
            $"The object-like macro '{name}' at {loc.FilePath}:{loc.LineNumber}:{loc.LineColumn} was not transpiled.";
    }
}
