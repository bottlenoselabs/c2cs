// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

public static partial class NativeRuntime
{
    /// <summary>
    ///     Gets the current <see cref="RuntimePlatform" />.
    /// </summary>
    public const RuntimePlatform RuntimePlatform =
#if WINDOWS
        global::RuntimePlatform.Windows;
#elif APPLE
        global::RuntimePlatform.macOS;
#elif LINUX
        global::RuntimePlatform.Linux;
#else
        global::RuntimePlatform.Unknown;
#endif

    /// <summary>
    ///     Gets the library file name extension for the current platform.
    /// </summary>
    public const string LibraryFileNameExtension =
#if WINDOWS
        ".dll";
#elif APPLE
        ".dylib";
#elif LINUX
        ".so";
#endif

    /// <summary>
    ///     Gets the library file name prefix for the current platform.
    /// </summary>
    public const string LibraryFileNamePrefix =
#if WINDOWS
        "";
#elif APPLE
        "lib";
#elif LINUX
        "lib";
#endif
}
