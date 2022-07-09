// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation.Diagnostics;

namespace C2CS.Contexts.WriteCodeCSharp.Domain.CodeGenerator.Diagnostics;

public sealed class CSharpCompileDiagnostic : Diagnostic
{
    public CSharpCompileDiagnostic(bool isError, Microsoft.CodeAnalysis.Diagnostic diagnostic)
        : base(isError ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning, CreateMessage(diagnostic))
    {
    }

    private static string CreateMessage(Microsoft.CodeAnalysis.Diagnostic diagnostic)
    {
        return $"C# code compilation diagnostic: ({diagnostic.ToString()}.";
    }
}
