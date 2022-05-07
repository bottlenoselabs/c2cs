// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using JetBrains.Annotations;

namespace C2CS;

[PublicAPI]
// ReSharper disable once RedundantAttributeUsageProperty
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class BindgenAttribute : Attribute
{
    public string? WorkingDirectory { get; set; }

    public string ConfigurationFileName { get; set; } = string.Empty;

    public string OutputLogFileName { get; set; } = string.Empty;
}
