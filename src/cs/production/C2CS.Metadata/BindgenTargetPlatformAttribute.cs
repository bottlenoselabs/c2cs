// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using JetBrains.Annotations;

namespace C2CS;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class BindgenTargetPlatformAttribute : Attribute
{
    public string Name { get; set; } = string.Empty;

    public string[] Frameworks { get; set; } = Array.Empty<string>();
}
