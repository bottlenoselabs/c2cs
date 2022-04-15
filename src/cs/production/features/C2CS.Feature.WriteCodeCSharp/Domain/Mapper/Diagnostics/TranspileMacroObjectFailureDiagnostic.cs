// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Feature.ReadCodeC.Data;
using C2CS.Feature.ReadCodeC.Data.Model;
using C2CS.Foundation.Diagnostics;

namespace C2CS.Feature.WriteCodeCSharp.Domain.Mapper.Diagnostics;

public sealed class TranspileMacroObjectFailureDiagnostic : Diagnostic
{
    public TranspileMacroObjectFailureDiagnostic(string name, CLocation location)
        : base(DiagnosticSeverity.Warning, CreateMessage(name, location))
    {
    }

    private static string CreateMessage(string name, CLocation location)
    {
        return $"The object-like macro '{name}' at {location.FilePath}:{location.LineNumber}:{location.LineColumn} failed to be transpiled.";
    }
}
