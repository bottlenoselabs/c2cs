// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;

namespace C2CS.Feature.BindgenCSharp.Domain;

public class DiagnosticMacroObjectAlreadyExists : Diagnostic
{
    public DiagnosticMacroObjectAlreadyExists(string name, CLocation loc)
        : base(DiagnosticSeverity.Warning)
    {
        Summary =
            $"The object-like macro '{name}' at {loc.FilePath}:{loc.LineNumber}:{loc.LineColumn} already previously exists.";
    }
}
