// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;

namespace C2CS.Feature.BindgenCSharp.Domain;

public class DiagnosticMacroObjectNotTranspiled : Diagnostic
{
    public DiagnosticMacroObjectNotTranspiled(string name, CLocation location)
        : base(DiagnosticSeverity.Warning, CreateMessage(name, location))
    {
    }

    private static string CreateMessage(string name, CLocation location)
    {
        return $"The object-like macro '{name}' at {location.FilePath}:{location.LineNumber}:{location.LineColumn} was not transpiled.";
    }
}
