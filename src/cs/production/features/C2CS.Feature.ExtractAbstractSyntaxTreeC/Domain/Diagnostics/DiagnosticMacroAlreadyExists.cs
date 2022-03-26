// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Diagnostics;

public sealed class DiagnosticMacroAlreadyExists : Diagnostic
{
    public DiagnosticMacroAlreadyExists(string name)
        : base(DiagnosticSeverity.Warning, CreateMessage(name))
    {
    }

    private static string CreateMessage(string name)
    {
        return $"A macro with the '{name}' already exists.";
    }
}
