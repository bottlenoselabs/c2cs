// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using bottlenoselabs.Common.Diagnostics;

namespace C2CS.Features.WriteCodeCSharp.Domain.CodeGenerator.Diagnostics;

public sealed class CSharpCompileDiagnostic : Diagnostic
{
    public CSharpCompileDiagnostic(string value)
        : base(DiagnosticSeverity.Error, CreateMessage(value))
    {
    }

    private static string CreateMessage(string value)
    {
        return $"- C# code compilation failure: {value}.";
    }
}
