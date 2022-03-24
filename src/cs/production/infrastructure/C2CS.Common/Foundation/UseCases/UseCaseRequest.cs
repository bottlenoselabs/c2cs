// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS;

/// <summary>
///     Represents un-sanitized input for execution of a <see cref="UseCase{TRequest,TInput,TResponse}" />.
/// </summary>
public class UseCaseRequest
{
    /// <summary>
    ///     The working directory to use. Default is <c>null</c>. If <c>null</c>, the current directory is used.
    /// </summary>
    public string? WorkingDirectory { get; set; }
}
