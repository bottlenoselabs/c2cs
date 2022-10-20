// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using JetBrains.Annotations;

namespace C2CS.Options;

/// <summary>
///     A pair of source and target names for renaming.
/// </summary>
[PublicAPI]
public sealed class WriterCSharpCodeOptionsMappedName
{
    /// <summary>
    ///     The name to rename.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    ///     The renamed name.
    /// </summary>
    public string Target { get; set; } = string.Empty;
}
