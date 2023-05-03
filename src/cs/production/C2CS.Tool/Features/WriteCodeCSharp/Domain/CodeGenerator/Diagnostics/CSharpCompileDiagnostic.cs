// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation;

namespace C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator.Diagnostics;

public sealed class CSharpCompileDiagnostic : Diagnostic
{
    public CSharpCompileDiagnostic(string filePath, Microsoft.CodeAnalysis.Diagnostic diagnostic)
        : base(DiagnosticSeverity.Error, CreateMessage(filePath, diagnostic))
    {
    }

    private static string CreateMessage(string filePath, Microsoft.CodeAnalysis.Diagnostic diagnostic)
    {
        return $"- C# code compilation diagnostic: {filePath} {diagnostic}.";
    }
}
