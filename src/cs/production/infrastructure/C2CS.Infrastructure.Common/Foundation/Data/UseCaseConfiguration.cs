// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;

namespace C2CS.Foundation.Data;

/// <summary>
///     Represents un-sanitized input for execution of a use case.
/// </summary>
public class UseCaseConfiguration
{
    /// <summary>
    ///     The working directory to use. Default is <c>null</c>. If <c>null</c>, the current directory is used.
    /// </summary>
    [JsonIgnore]
    public string? WorkingDirectory { get; set; }
}
