// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain;

public class DiagnosticPlatformMismatch : Diagnostic
{
    public DiagnosticPlatformMismatch(string actualPlatformName, string expectedPlatformName)
        : base(DiagnosticSeverity.Error, CreateMessage(actualPlatformName, expectedPlatformName))
    {
    }

    private static string CreateMessage(string actualPlatformName, string expectedPlatformName)
    {
        return $"The C library was expected to have runtime of platform '{expectedPlatformName}' but the header file specified a runtime platform of '{actualPlatformName}'.";
    }
}
