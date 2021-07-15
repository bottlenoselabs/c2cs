// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;

namespace C2CS
{
    public static partial class Runtime
    {
        /// <summary>
        ///     Gets the current <see cref="RuntimeOperatingSystem" />.
        /// </summary>
        public static RuntimeOperatingSystem OperatingSystem => GetRuntimeOperatingSystem();

        /// <summary>
        ///     Gets the current <see cref="RuntimeArchitecture" />.
        /// </summary>
        public static RuntimeArchitecture Architecture => GetRuntimeArchitecture();

        /// <summary>
        ///     Gets the library file name extension given a <see cref="RuntimeOperatingSystem" />.
        /// </summary>
        /// <param name="operatingSystem">The runtime platform.</param>
        /// <returns>A <see cref="string" /> containing the library file name extension for the <paramref name="operatingSystem" />.</returns>
        /// <exception cref="NotImplementedException"><paramref name="operatingSystem" /> is not available yet with .NET 5.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="operatingSystem"/> is not a known valid value.</exception>
        public static string LibraryFileNameExtension(RuntimeOperatingSystem operatingSystem)
        {
            switch (operatingSystem)
            {
                case RuntimeOperatingSystem.Windows:
                    return ".dll";
                case RuntimeOperatingSystem.macOS:
                case RuntimeOperatingSystem.tvOS:
                    return ".dylib";
                case RuntimeOperatingSystem.Linux:
                case RuntimeOperatingSystem.FreeBSD:
                case RuntimeOperatingSystem.Android:
                    return ".so";
                case RuntimeOperatingSystem.Browser:
                case RuntimeOperatingSystem.PlayStation:
                case RuntimeOperatingSystem.Xbox:
                    throw new InvalidOperationException("Dynamic linking of a library is not possible for Xbox.");
                case RuntimeOperatingSystem.iOS:
                    throw new InvalidOperationException("Dynamic linking of a library is not possible for iOS.");
                case RuntimeOperatingSystem.Unknown:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(operatingSystem), operatingSystem, null);
            }
        }

        /// <summary>
        ///     Gets the library file name prefix for a <see cref="RuntimeOperatingSystem" />.
        /// </summary>
        /// <param name="targetOperatingSystem">The runtime platform.</param>
        /// <returns>A <see cref="string" /> containing the library file name prefix for the <paramref name="targetOperatingSystem" />.</returns>
        /// <exception cref="NotImplementedException"><paramref name="targetOperatingSystem" /> is not available yet with .NET 5.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="targetOperatingSystem"/> is not a known valid value.</exception>
        public static string LibraryFileNamePrefix(RuntimeOperatingSystem targetOperatingSystem)
        {
            switch (targetOperatingSystem)
            {
                case RuntimeOperatingSystem.Windows:
                    return string.Empty;
                case RuntimeOperatingSystem.macOS:
                case RuntimeOperatingSystem.tvOS:
                case RuntimeOperatingSystem.iOS:
                case RuntimeOperatingSystem.Linux:
                case RuntimeOperatingSystem.FreeBSD:
                case RuntimeOperatingSystem.Android:
                    return "lib";
                case RuntimeOperatingSystem.Browser:
                    throw new NotImplementedException();
                case RuntimeOperatingSystem.Unknown:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetOperatingSystem), targetOperatingSystem, null);
            }
        }

        private static RuntimeArchitecture GetRuntimeArchitecture()
        {
            return RuntimeInformation.OSArchitecture switch
            {
                System.Runtime.InteropServices.Architecture.Arm64 => RuntimeArchitecture.ARM64,
                System.Runtime.InteropServices.Architecture.Arm => RuntimeArchitecture.ARM32,
                System.Runtime.InteropServices.Architecture.X86 => RuntimeArchitecture.X86,
                System.Runtime.InteropServices.Architecture.X64 => RuntimeArchitecture.X64,
                System.Runtime.InteropServices.Architecture.Wasm => RuntimeArchitecture.Unknown,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static RuntimeOperatingSystem GetRuntimeOperatingSystem()
        {
            if (System.OperatingSystem.IsWindows())
            {
                return RuntimeOperatingSystem.Windows;
            }

            if (System.OperatingSystem.IsMacOS())
            {
                return RuntimeOperatingSystem.macOS;
            }

            if (System.OperatingSystem.IsLinux())
            {
                return RuntimeOperatingSystem.Linux;
            }

            if (System.OperatingSystem.IsAndroid())
            {
                return RuntimeOperatingSystem.Android;
            }

            if (System.OperatingSystem.IsIOS())
            {
                return RuntimeOperatingSystem.iOS;
            }

            if (System.OperatingSystem.IsTvOS())
            {
                return RuntimeOperatingSystem.tvOS;
            }

            if (System.OperatingSystem.IsBrowser())
            {
                return RuntimeOperatingSystem.Browser;
            }

            return RuntimeOperatingSystem.Unknown;
        }
    }
}
