// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace C2CS;

/// <summary>
///     The collection of utilities for interoperability with native libraries in C#. Used by code which is generated
///     using the C2CS tool: https://github.com/lithiumtoast/c2cs.
/// </summary>
[PublicAPI]
[SuppressMessage(
    "Microsoft.Naming",
    "CA1724:TypeNamesShouldNotMatchNamespaces",
    Justification = "It's okay because it's under the namespace `C2CS`.")]
public static partial class Runtime
{
}
