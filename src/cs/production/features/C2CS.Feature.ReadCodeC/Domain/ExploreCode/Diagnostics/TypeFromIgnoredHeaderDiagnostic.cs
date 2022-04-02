// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation.Diagnostics;

namespace C2CS.Feature.ReadCodeC.Domain.ExploreCode.Diagnostics;

public sealed class TypeFromIgnoredHeaderDiagnostic : Diagnostic
{
    public TypeFromIgnoredHeaderDiagnostic(string typeName, string headerFilePath)
        : base(DiagnosticSeverity.Warning, CreateMessage(typeName, headerFilePath))
    {
    }

    private static string CreateMessage(string typeName, string headerFilePath)
    {
        return $"The type '{typeName}' belongs to the ignored header file '{headerFilePath}', but is used in the abstract syntax tree.";
    }
}
