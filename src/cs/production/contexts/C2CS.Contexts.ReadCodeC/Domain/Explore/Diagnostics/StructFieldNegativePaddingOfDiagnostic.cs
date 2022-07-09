// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Contexts.ReadCodeC.Data.Model;
using C2CS.Foundation.Diagnostics;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Diagnostics;

public sealed class StructFieldNegativePaddingOfDiagnostic : Diagnostic
{
    public StructFieldNegativePaddingOfDiagnostic(string fieldName, CLocation location, int paddingOf)
        : base(DiagnosticSeverity.Error, CreateMessage(fieldName, location, paddingOf))
    {
    }

    private static string CreateMessage(string fieldName, CLocation location, int paddingOf)
    {
        return $"{location}: The field '{fieldName}' has negative padding of '{paddingOf}'.";
    }
}
