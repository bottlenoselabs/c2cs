// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace C2CS.Tools
{
    /// <summary>
    ///     The collection of utilities for interoperability with native libraries in C#.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global", Justification = "Public API.")]
    public static partial class NativeTools
    {
        /// <summary>
        ///     Gets the current <see cref="NativeRuntimePlatform" />.
        /// </summary>
        public static NativeRuntimePlatform RuntimePlatform { get; } = GetRuntimePlatform();

        private static NativeRuntimePlatform GetRuntimePlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return NativeRuntimePlatform.Windows;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return NativeRuntimePlatform.macOS;
            }

            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return NativeRuntimePlatform.Linux;
            }

            // TODO: iOS, Android, etc

            return NativeRuntimePlatform.Unknown;
        }
    }
}
