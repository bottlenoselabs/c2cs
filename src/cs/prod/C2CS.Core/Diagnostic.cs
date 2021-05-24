// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using JetBrains.Annotations;

namespace C2CS
{
    /// <summary>
    ///     Program runtime feedback.
    /// </summary>
    [PublicAPI]
    public abstract class Diagnostic
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Diagnostic" /> class.
        /// </summary>
        /// <param name="severity">The severity.</param>
        protected Diagnostic(DiagnosticSeverity severity)
        {
            Severity = severity;
        }

        /// <summary>
        ///     The severity of the program's runtime feedback.
        /// </summary>
        public DiagnosticSeverity Severity { get; }

        /// <summary>
        ///     The short, one sentence long, description of the program's runtime feedback.
        /// </summary>
        public string? Summary { get; protected set; }

        public override string ToString()
        {
            return $"{GetDiagnosticSeverityShortString()}: {Summary}";
        }

        private string GetDiagnosticSeverityShortString()
        {
            return Severity switch
            {
                DiagnosticSeverity.Information => "INFO",
                DiagnosticSeverity.Error => "ERROR",
                DiagnosticSeverity.Warning => "WARN",
                DiagnosticSeverity.Panic => "PANIC",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
