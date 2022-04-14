// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation.Diagnostics;

namespace C2CS.Feature.ReadCodeC.Domain.ExploreCode.Diagnostics;

public sealed class PlatformMismatchDiagnostic : Diagnostic
{
    public PlatformMismatchDiagnostic(TargetPlatform actualPlatform, TargetPlatform expectedPlatform)
        : base(DiagnosticSeverity.Error, CreateMessage(actualPlatform, expectedPlatform))
    {
    }

    private static string CreateMessage(TargetPlatform actualPlatform, TargetPlatform expectedPlatform)
    {
        return $"The C library was expected to have runtime of platform '{expectedPlatform}' but the header file specified a runtime platform of '{actualPlatform}'.";
    }
}
