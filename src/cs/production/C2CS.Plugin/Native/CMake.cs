// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS;

public static class CMake
{
    public static bool Build(string rootDirectory, string cMakeDirectoryPath, string libraryOutputDirectoryPath)
    {
        if (!Directory.Exists(rootDirectory))
        {
            throw new DirectoryNotFoundException(cMakeDirectoryPath);
        }

        if (!Directory.Exists(cMakeDirectoryPath))
        {
            throw new DirectoryNotFoundException(cMakeDirectoryPath);
        }

        var libraryOutputDirectoryPathNormalized =
            libraryOutputDirectoryPath.Replace("\\", "/", StringComparison.InvariantCulture);
        var result =
            $"cmake -S . -B cmake-build-release -DCMAKE_BUILD_TYPE=Release -DCMAKE_ARCHIVE_OUTPUT_DIRECTORY={libraryOutputDirectoryPathNormalized} -DCMAKE_LIBRARY_OUTPUT_DIRECTORY={libraryOutputDirectoryPathNormalized} -DCMAKE_RUNTIME_OUTPUT_DIRECTORY={libraryOutputDirectoryPathNormalized} -DCMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE={libraryOutputDirectoryPathNormalized}"
                .ExecuteShell(cMakeDirectoryPath, windowsUsePowerShell: false);
        if (result.ExitCode != 0)
        {
            Console.Write(result.Output);
            return false;
        }

        result = "cmake --build cmake-build-release --config Release"
            .ExecuteShell(cMakeDirectoryPath, windowsUsePowerShell: false);
        if (result.ExitCode != 0)
        {
            Console.Write(result.Output);
            return false;
        }

        var outputDirectoryPath = Path.Combine(cMakeDirectoryPath, "lib");
        if (!Directory.Exists(outputDirectoryPath))
        {
            return false;
        }

        var operatingSystem = Native.OperatingSystem;
        var libraryFileNameExtension = Native.LibraryFileNameExtension(operatingSystem);
        var outputFilePaths = Directory.EnumerateFiles(
            outputDirectoryPath, $"*{libraryFileNameExtension}", SearchOption.AllDirectories);
        foreach (var outputFilePath in outputFilePaths)
        {
            var targetFilePath = outputFilePath.Replace(
                outputDirectoryPath, libraryOutputDirectoryPath, StringComparison.InvariantCulture);
            var targetFileName = Path.GetFileName(targetFilePath);

            if (operatingSystem == NativeOperatingSystem.Windows)
            {
                if (targetFileName.StartsWith("lib", StringComparison.InvariantCulture))
                {
                    targetFileName = targetFileName[3..];
                }
            }

            var targetFileDirectoryPath = Path.GetDirectoryName(targetFilePath)!;
            targetFilePath = Path.Combine(targetFileDirectoryPath, targetFileName);
            if (!Directory.Exists(targetFileDirectoryPath))
            {
                Directory.CreateDirectory(targetFileDirectoryPath);
            }

            if (File.Exists(targetFilePath))
            {
                File.Delete(targetFilePath);
            }

            File.Copy(outputFilePath, targetFilePath);
        }

        Directory.Delete(outputDirectoryPath, true);
        Directory.Delete($"{cMakeDirectoryPath}/cmake-build-release", true);

        return true;
    }
}
