// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Diagnostics;

public class TypeFromIgnoredHeaderDiagnostic : Diagnostic
{
    public TypeFromIgnoredHeaderDiagnostic(string typeName, string headerFilePath)
        : base(DiagnosticSeverity.Warning)
    {
        Summary =
            $"The type '{typeName}' belongs to the ignored header file '{headerFilePath}', but is used in the abstract syntax tree.";
    }
}
