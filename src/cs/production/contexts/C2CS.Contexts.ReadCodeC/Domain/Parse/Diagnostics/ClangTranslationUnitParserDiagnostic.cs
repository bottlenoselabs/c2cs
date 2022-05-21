// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation.Diagnostics;

namespace C2CS.Contexts.ReadCodeC.Domain.Parse.Diagnostics;

public sealed class ClangTranslationUnitParserDiagnostic : Diagnostic
{
    public ClangTranslationUnitParserDiagnostic(DiagnosticSeverity severity, string message)
        : base(severity, message)
    {
    }
}
