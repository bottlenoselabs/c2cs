// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using JetBrains.Annotations;

namespace C2CS.Foundation.Diagnostics;

/// <summary>
///     Program runtime feedback that is not necessarily a run-time exception.
/// </summary>
[PublicAPI]
public abstract class Diagnostic
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Diagnostic" /> class.
    /// </summary>
    /// <param name="severity">The severity of the <see cref="Diagnostic" />.</param>
    /// <param name="message">The message of the <see cref="Diagnostic" />.</param>
    protected Diagnostic(DiagnosticSeverity severity, string message)
    {
        Severity = severity;
        Message = message;
    }

    /// <summary>
    ///     The severity of the <see cref="Diagnostic" />.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    ///     The message of the <see cref="Diagnostic" />.
    /// </summary>
    public string Message { get; } = string.Empty;

    protected internal string GetName()
    {
        var type = GetType();
        var typeName = type.Name;
        if (!typeName.StartsWith("Diagnostic", StringComparison.InvariantCulture))
        {
            return typeName;
        }

        return typeName.Replace("Diagnostic", string.Empty, StringComparison.InvariantCulture);
    }
}
