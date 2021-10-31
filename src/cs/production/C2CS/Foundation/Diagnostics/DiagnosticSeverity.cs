// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using JetBrains.Annotations;

namespace C2CS;

/// <summary>
///     Defines different levels of program runtime feedback.
/// </summary>
[PublicAPI]
public enum DiagnosticSeverity
{
    /// <summary>
    ///     Verbose and ignorable.
    /// </summary>
    Information,

    /// <summary>
    ///     Suspicious; indicative of an expected but possibly undesired outcome. Does not halt the program.
    /// </summary>
    Warning,

    /// <summary>
    ///     Unacceptable; indicative of an unexpected result. Does not halt the program.
    /// </summary>
    Error,

    /// <summary>
    ///     Crash; gracefully exit the program with a stack trace.
    /// </summary>
    Panic
}
