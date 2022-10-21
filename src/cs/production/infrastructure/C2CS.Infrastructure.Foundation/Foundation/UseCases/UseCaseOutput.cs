// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Foundation.UseCases;

public abstract class UseCaseOutput<TInput>
{
    public bool IsSuccess { get; private set; }

    public TInput Input { get; internal set; } = default!;

    public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

    internal void Complete(ImmutableArray<Diagnostic> diagnostics)
    {
        Diagnostics = diagnostics;
        IsSuccess = CalculateIsSuccessful(diagnostics);
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
