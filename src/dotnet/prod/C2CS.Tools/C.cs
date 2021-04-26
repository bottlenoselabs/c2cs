// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Reflection;

namespace C2CS.Tools
{
    public static class C
    {
        public static void CMake(string baseDirectoryPath)
        {
            var currentApplicationBaseDirectoryPath = Assembly.GetEntryAssembly()!.Location;

            "cmake -S . -B build-temp -G 'Unix Makefiles' -DCMAKE_BUILD_TYPE=Release".Bash(baseDirectoryPath);
            "make -C ./build-temp".Bash(baseDirectoryPath);
            $"cp -a /build-temp/. {currentApplicationBaseDirectoryPath}".Bash(baseDirectoryPath);
            "rm -rf ./build-temp".Bash(baseDirectoryPath);
        }
    }
}
