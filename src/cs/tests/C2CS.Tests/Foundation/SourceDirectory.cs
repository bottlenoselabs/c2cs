// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Runtime.CompilerServices;

namespace C2CS.Tests.Foundation;

public static class SourceDirectory
{
    public static string Path { get; private set; } = string.Empty;

    public static void SetPath([CallerFilePath] string? sourceCodeFilePath = null)
    {
        Path = System.IO.Path.GetDirectoryName(sourceCodeFilePath!)!;
    }
}
