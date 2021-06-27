// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using JetBrains.Annotations;

namespace C2CS
{
    [PublicAPI]
    public static class Terminal
    {
        public static string ShCaptureStandardOutput(this string command, string? workingDirectory = null, string? fileName = null)
        {
            var process = CreateProcess(command, workingDirectory, fileName);

            process.Start();
            process.WaitForExit();

            var result = process.StandardOutput.ReadToEnd().Trim('\n', '\r');
            return result;
        }

        public static bool Sh(this string command, string? workingDirectory, string? fileName = null)
        {
            var process = CreateProcess(command, workingDirectory, fileName);

            process.OutputDataReceived += OnProcessOnOutputDataReceived;
            process.ErrorDataReceived += OnProcessOnErrorDataReceived;

            process.Start();

            while (!process.HasExited)
            {
                Thread.Sleep(100);

                try
                {
                    process.BeginOutputReadLine();
                }
                catch (InvalidOperationException)
                {
                    process.CancelOutputRead();
                }

                try
                {
                    process.BeginErrorReadLine();
                }
                catch (InvalidOperationException)
                {
                    process.CancelErrorRead();
                }
            }

            return process.ExitCode == 0;
        }

        private static void OnProcessOnErrorDataReceived(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine(args.Data);
        }

        private static void OnProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            Console.WriteLine(args.Data);
        }

        private static Process CreateProcess(string command, string? workingDirectory, string? fileName)
        {
            if (workingDirectory != null && !Directory.Exists(workingDirectory))
            {
                throw new DirectoryNotFoundException(workingDirectory);
            }

            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory ?? Environment.CurrentDirectory,
                CreateNoWindow = true,
            };

            if (!string.IsNullOrEmpty(fileName))
            {
                processStartInfo.FileName = fileName;
                processStartInfo.Arguments = command;
            }
            else
            {
                var platform = Runtime.Platform;
                if (platform == RuntimePlatform.Windows)
                {
                    processStartInfo.FileName = "wsl";
                    processStartInfo.Arguments = command;
                }
                else
                {
                    processStartInfo.FileName = "bash";
                    var escapedArgs = command.Replace($"\"", $"\\\"");
                    processStartInfo.Arguments = $"-c \"{escapedArgs}\"";
                }
            }

            var process = new Process
            {
                StartInfo = processStartInfo
            };
            return process;
        }

        public static bool CMake(string rootDirectory, string cMakeDirectoryPath, string targetLibraryDirectoryPath)
        {
            if (!Directory.Exists(rootDirectory))
            {
                throw new DirectoryNotFoundException(cMakeDirectoryPath);
            }

            if (!Directory.Exists(cMakeDirectoryPath))
            {
                throw new DirectoryNotFoundException(cMakeDirectoryPath);
            }

            var cMakeCommand = "cmake -S . -B cmake-build-release -G 'Unix Makefiles' -DCMAKE_BUILD_TYPE=Release";

            var platform = Runtime.Platform;
            if (platform == RuntimePlatform.Windows)
            {
                var toolchainFilePath = WindowsToLinuxPath($"{rootDirectory}/src/c/mingw-w64-x86_64.cmake");
                cMakeCommand += $" -DCMAKE_TOOLCHAIN_FILE='{toolchainFilePath}'";
            }

            var isSuccess = cMakeCommand.Sh(cMakeDirectoryPath);
            if (!isSuccess)
            {
                return false;
            }

            isSuccess = "make -C ./cmake-build-release".Sh(cMakeDirectoryPath);
            if (!isSuccess)
            {
                return false;
            }

            var outputDirectoryPath = Path.Combine(cMakeDirectoryPath, "lib");
            if (!Directory.Exists(outputDirectoryPath))
            {
                return false;
            }

            var runtimePlatform = Runtime.Platform;
            var libraryFileNameExtension = Runtime.LibraryFileNameExtension(runtimePlatform);
            var outputFilePaths = Directory.EnumerateFiles(
                outputDirectoryPath, $"*{libraryFileNameExtension}", SearchOption.AllDirectories);
            foreach (var outputFilePath in outputFilePaths)
            {
                var targetFilePath = outputFilePath.Replace(outputDirectoryPath, targetLibraryDirectoryPath);
                var targetFileName = Path.GetFileName(targetFilePath);

                if (platform == RuntimePlatform.Windows)
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

        private static string WindowsToLinuxPath(string path)
        {
            var pathWindows = Path.GetFullPath(path);
            var pathRootWindows = Path.GetPathRoot(pathWindows)!;
            var pathRootLinux = $"/mnt/{pathRootWindows.ToLower(CultureInfo.InvariantCulture)[0]}/";
            var pathLinux = pathWindows
                .Replace(pathRootWindows, pathRootLinux)
                .Replace('\\', '/');
            return pathLinux;
        }
    }
}
