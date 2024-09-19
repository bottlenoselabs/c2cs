// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using bottlenoselabs.Common.Diagnostics;

namespace C2CS.WriteCodeCSharp.CodeGenerator.Diagnostics;

public class CSharpCompileSkipDiagnostic : Diagnostic
{
    public CSharpCompileSkipDiagnostic(string reason)
        : base(DiagnosticSeverity.Warning, CreateMessage(reason))
    {
    }

    private static string CreateMessage(string reason)
    {
        return $"- Skipped C# code compilation: {reason}";
    }
}
