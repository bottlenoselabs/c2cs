// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Data.C.Model;

namespace C2CS.ReadCodeC.Explore.Diagnostics;

public sealed class TypeFromBlockedHeaderDiagnostic : Diagnostic
{
    public TypeFromBlockedHeaderDiagnostic(string typeName, CLocation location, CLocation rootLocation)
        : base(DiagnosticSeverity.Warning, CreateMessage(typeName, location, rootLocation))
    {
    }

    private static string CreateMessage(string typeName, CLocation location, CLocation rootLocation)
    {
        return
            $"{rootLocation}: The type '{typeName}' belongs to the blocked header file '{location}' but it is used anyways for bindgen. If you can't or don't want to modify the C header files consider using 'pass_through_types' to suppress this diagnostic.";
    }
}
