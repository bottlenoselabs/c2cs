// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Options;

/// <summary>
///     Represents un-sanitized input for execution of a use case.
/// </summary>
[PublicAPI]
public class UseCaseOptions
{
    /// <summary>
    ///     The working directory to use.
    /// </summary>
    /// <remarks>
    ///     <para>Default is <c>null</c>. If <c>null</c>, the current directory is used.</para>
    /// </remarks>
    [JsonIgnore]
    public string? WorkingFileDirectory { get; set; }
}
