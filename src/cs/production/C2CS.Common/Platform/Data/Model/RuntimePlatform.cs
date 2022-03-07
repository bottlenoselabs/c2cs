// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;
using C2CS.Serialization;
using JetBrains.Annotations;

namespace C2CS;

/// <summary>
///     Defines the native runtime operating systems.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(RuntimePlatformJsonConverter))]
public partial record struct RuntimePlatform
{
    /// <summary>
    ///     The runtime operating system.
    /// </summary>
    public RuntimeOperatingSystem OperatingSystem;

    /// <summary>
    ///     The runtime computer architecture.
    /// </summary>
    public RuntimeArchitecture Architecture;

    /// <summary>
    ///     Unknown runtime platform.
    /// </summary>
    public static readonly RuntimePlatform Unknown = new(RuntimeOperatingSystem.Unknown, RuntimeArchitecture.Unknown);

#pragma warning disable SA1300
#pragma warning disable SA1310
#pragma warning disable SA1311
#pragma warning disable SA1307
#pragma warning disable CA2211
#pragma warning disable CA1707
#pragma warning disable IDE1006
    // ReSharper disable InconsistentNaming

    public static readonly RuntimePlatform win_x64 = new(RuntimeOperatingSystem.Windows, RuntimeArchitecture.X64);
    public static readonly RuntimePlatform osx_arm64 = new(RuntimeOperatingSystem.macOS, RuntimeArchitecture.ARM64);
    public static readonly RuntimePlatform osx_x64 = new(RuntimeOperatingSystem.macOS, RuntimeArchitecture.X64);
    public static readonly RuntimePlatform linux_x64 = new(RuntimeOperatingSystem.Linux, RuntimeArchitecture.X64);

    // ReSharper restore InconsistentNaming
#pragma warning restore IDE1006
#pragma warning restore CA1707
#pragma warning restore CA2211
#pragma warning restore SA1311
#pragma warning restore SA1307
#pragma warning restore SA1310
#pragma warning restore SA1300

    internal RuntimePlatform(RuntimeOperatingSystem operatingSystem, RuntimeArchitecture architecture)
    {
        OperatingSystem = operatingSystem;
        Architecture = architecture;
    }

    public override string ToString()
    {
        return ToString(this);
    }
}
