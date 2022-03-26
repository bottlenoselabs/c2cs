// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS;

public abstract class UseCaseResponse
{
    public bool IsSuccessful { get; internal set; }

    public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

    internal void WithDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
        IsSuccessful = CalculateIsSuccessful(diagnostics);
    }

    private static bool CalculateIsSuccessful(ImmutableArray<Diagnostic> diagnostics)
    {
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Severity is
                DiagnosticSeverity.Error or
                DiagnosticSeverity.Panic)
            {
                return false;
            }
        }

        return true;
    }
}
