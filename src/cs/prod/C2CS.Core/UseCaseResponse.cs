// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS
{
    public abstract class UseCaseResponse
    {
        public UseCaseOutputStatus Status { get; private set; }

        public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

        internal void WithDiagnostics(ImmutableArray<Diagnostic> diagnostics)
        {
            Diagnostics = diagnostics;
            Status = CalculateStatus(diagnostics);
        }

        private static UseCaseOutputStatus CalculateStatus(ImmutableArray<Diagnostic> diagnostics)
        {
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error ||
                    diagnostic.Severity == DiagnosticSeverity.Panic)
                {
                    return UseCaseOutputStatus.Failure;
                }
            }

            return UseCaseOutputStatus.Success;
        }
    }
}
