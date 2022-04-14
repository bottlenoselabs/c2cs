// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using C2CS.Foundation.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace C2CS.Foundation.Diagnostics;

/// <summary>
///     Program runtime feedback that is not necessarily a run-time exception.
/// </summary>
[PublicAPI]
public abstract class Diagnostic
{
    private readonly Action<ILogger, Exception> _actionLogDiagnostic;

    /// <summary>
    ///     The severity of the <see cref="Diagnostic" />.
    /// </summary>
    public DiagnosticSeverity Severity { get; }

    /// <summary>
    ///     The message of the <see cref="Diagnostic" />.
    /// </summary>
    public string Message { get; } = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Diagnostic" /> class.
    /// </summary>
    /// <param name="severity">The severity of the <see cref="Diagnostic" />.</param>
    /// <param name="message">The message of the <see cref="Diagnostic" />.</param>
    protected Diagnostic(DiagnosticSeverity severity, string message)
    {
        Severity = severity;

        var logLevel = severity switch
        {
            DiagnosticSeverity.Information => LogLevel.Information,
            DiagnosticSeverity.Warning => LogLevel.Warning,
            DiagnosticSeverity.Error => LogLevel.Error,
            DiagnosticSeverity.Panic => LogLevel.Critical,
            _ => LogLevel.None
        };

        var name = GetName();
        _actionLogDiagnostic = LoggerMessage.Define(
            logLevel,
            LoggingEventRegistry.CreateEventIdentifier(name),
            $"- {name} {message}");
    }

    protected string GetName()
    {
        var type = GetType();
        var typeName = type.Name;
        if (!typeName.StartsWith("Diagnostic", StringComparison.InvariantCulture))
        {
            return typeName;
        }

        return typeName.Replace("Diagnostic", string.Empty, StringComparison.InvariantCulture);
    }

    internal void Log(ILogger logger)
    {
        _actionLogDiagnostic(logger, null!);
    }
}
