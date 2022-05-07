// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation.Diagnostics;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Diagnostics;

public sealed class MacroAlreadyExistsDiagnostic : Diagnostic
{
    public MacroAlreadyExistsDiagnostic(string name)
        : base(DiagnosticSeverity.Warning, CreateMessage(name))
    {
    }

    private static string CreateMessage(string name)
    {
        return $"A macro with the '{name}' already exists.";
    }
}
