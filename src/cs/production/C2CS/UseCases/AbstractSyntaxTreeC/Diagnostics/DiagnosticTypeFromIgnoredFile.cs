// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    public class DiagnosticTypeFromIgnoredFile : Diagnostic
    {
        public DiagnosticTypeFromIgnoredFile(string typeName, string filePath)
            : base(DiagnosticSeverity.Warning)
        {
            Summary = $"The type '{typeName}' belongs to the ignored file '{filePath}', but is used in the abstract syntax tree.";
        }
    }
}
