// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Generic;

namespace C2CS.WriteCodeCSharp.Domain.CodeGenerator.Diagnostics;

public sealed class CSharpMergePlatformNodesDiagnostic : Diagnostic
{
    public CSharpMergePlatformNodesDiagnostic(string name, IEnumerable<TargetPlatform> targetPlatforms)
        : base(DiagnosticSeverity.Error, CreateMessage(name, targetPlatforms))
    {
    }

    private static string CreateMessage(string name, IEnumerable<TargetPlatform> targetPlatforms)
    {
        var targetPlatformsString = string.Join(',', targetPlatforms);
        return $"Failed to merge platform nodes ('{targetPlatformsString}') for '{name}'.";
    }
}
