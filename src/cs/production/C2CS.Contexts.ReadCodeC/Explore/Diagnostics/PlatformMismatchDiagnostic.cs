// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Contexts.ReadCodeC.Explore.Diagnostics;

public sealed class PlatformMismatchDiagnostic : Diagnostic
{
    public PlatformMismatchDiagnostic(TargetPlatform actual, TargetPlatform expected)
        : base(DiagnosticSeverity.Error, CreateMessage(actual, expected))
    {
    }

    private static string CreateMessage(TargetPlatform actual, TargetPlatform expected)
    {
        return $"The C header file was expected be for platform '{expected}' but was for '{actual}'.";
    }
}
