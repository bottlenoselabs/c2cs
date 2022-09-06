// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation.Diagnostics;

namespace C2CS.Contexts.ReadCodeC.Parse.Diagnostics;

public sealed class MissingSystemHeadersDiagnostic : Diagnostic
{
    public MissingSystemHeadersDiagnostic(TargetPlatform targetPlatform)
        : base(DiagnosticSeverity.Error, CreateMessage(targetPlatform))
    {
    }

    private static string CreateMessage(TargetPlatform targetPlatform)
    {
        return $"No system headers were used or automatically found for '{targetPlatform}'.";
    }
}
