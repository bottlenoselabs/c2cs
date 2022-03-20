// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Diagnostics;

public class PlatformMismatchDiagnostic : Diagnostic
{
    public PlatformMismatchDiagnostic(string actualPlatformName, string expectedPlatformName)
        : base(DiagnosticSeverity.Error)
    {
        Summary =
            $"The C library was expected to have runtime of platform '{expectedPlatformName}' but the header file specified a runtime platform of '{actualPlatformName}'.";
    }
}
