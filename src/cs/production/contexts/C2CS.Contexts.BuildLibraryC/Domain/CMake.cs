// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

// namespace C2CS.Contexts.BuildLibraryC.Domain.Logic;
//
// public static class CMake
// {
//     public static bool GenerateBuildFiles(string cMakeDirectoryPath, string libraryOutputDirectoryPath)
//     {
//         if (!Directory.Exists(cMakeDirectoryPath))
//         {
//             throw new DirectoryNotFoundException(cMakeDirectoryPath);
//         }
//
//         var outPath = libraryOutputDirectoryPath.Replace("\\", "/", StringComparison.InvariantCulture);
//         var shellCommand =
//             $"cmake -S . -B cmake-build-release -DCMAKE_BUILD_TYPE=Release -DCMAKE_ARCHIVE_OUTPUT_DIRECTORY={outPath} -DCMAKE_LIBRARY_OUTPUT_DIRECTORY={outPath} -DCMAKE_RUNTIME_OUTPUT_DIRECTORY={outPath} -DCMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE={outPath}";
//
//         var isSuccess = shellCommand.Shell(cMakeDirectoryPath, windowsUsePowerShell: false);
//         if (!isSuccess)
//         {
//             return false;
//         }
//
//         isSuccess = "cmake --build cmake-build-release --config Release"
//             .Shell(cMakeDirectoryPath, windowsUsePowerShell: false);
//         if (!isSuccess)
//         {
//             return false;
//         }
//
//         var outputDirectoryPath = Path.Combine(cMakeDirectoryPath, "lib");
//         if (!Directory.Exists(outputDirectoryPath))
//         {
//             return false;
//         }
//
//         var runtimePlatform = Platform.HostOperatingSystem;
//         var libraryFileNameExtension = Platform.LibraryFileNameExtension(runtimePlatform);
//         var outputFilePaths = Directory.EnumerateFiles(
//             outputDirectoryPath, $"*{libraryFileNameExtension}", SearchOption.AllDirectories);
//         foreach (var outputFilePath in outputFilePaths)
//         {
//             var targetFilePath = outputFilePath.Replace(
//                     outputDirectoryPath, libraryOutputDirectoryPath, StringComparison.InvariantCulture);
//             var targetFileName = Path.GetFileName(targetFilePath);
//
//             if (runtimePlatform == RuntimeOperatingSystem.Windows)
//             {
//                 if (targetFileName.StartsWith("lib", StringComparison.InvariantCulture))
//                 {
//                     targetFileName = targetFileName[3..];
//                 }
//             }
//
//             var targetFileDirectoryPath = Path.GetDirectoryName(targetFilePath)!;
//             targetFilePath = Path.Combine(targetFileDirectoryPath, targetFileName);
//             if (!Directory.Exists(targetFileDirectoryPath))
//             {
//                 Directory.CreateDirectory(targetFileDirectoryPath);
//             }
//
//             if (File.Exists(targetFilePath))
//             {
//                 File.Delete(targetFilePath);
//             }
//
//             File.Copy(outputFilePath, targetFilePath);
//         }
//
//         Directory.Delete(outputDirectoryPath, true);
//         Directory.Delete($"{cMakeDirectoryPath}/cmake-build-release", true);
//
//         return true;
//     }
//
//     public static bool Build(string rootDirectory, string cMakeDirectoryPath, string libraryOutputDirectoryPath)
//     {
//         if (!Directory.Exists(rootDirectory))
//         {
//             throw new DirectoryNotFoundException(cMakeDirectoryPath);
//         }
//
//         if (!Directory.Exists(cMakeDirectoryPath))
//         {
//             throw new DirectoryNotFoundException(cMakeDirectoryPath);
//         }
//
//         var libraryOutputDirectoryPathNormalized = libraryOutputDirectoryPath.Replace("\\", "/", StringComparison.InvariantCulture);
//         var isSuccess = $"cmake -S . -B cmake-build-release -DCMAKE_BUILD_TYPE=Release -DCMAKE_ARCHIVE_OUTPUT_DIRECTORY={libraryOutputDirectoryPathNormalized} -DCMAKE_LIBRARY_OUTPUT_DIRECTORY={libraryOutputDirectoryPathNormalized} -DCMAKE_RUNTIME_OUTPUT_DIRECTORY={libraryOutputDirectoryPathNormalized} -DCMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE={libraryOutputDirectoryPathNormalized}"
//             .Shell(cMakeDirectoryPath, windowsUsePowerShell: false);
//         if (!isSuccess)
//         {
//             return false;
//         }
//
//         isSuccess = "cmake --build cmake-build-release --config Release"
//             .Shell(cMakeDirectoryPath, windowsUsePowerShell: false);
//         if (!isSuccess)
//         {
//             return false;
//         }
//
//         var outputDirectoryPath = Path.Combine(cMakeDirectoryPath, "lib");
//         if (!Directory.Exists(outputDirectoryPath))
//         {
//             return false;
//         }
//
//         var runtimePlatform = Platform.HostOperatingSystem;
//         var libraryFileNameExtension = Platform.LibraryFileNameExtension(runtimePlatform);
//         var outputFilePaths = Directory.EnumerateFiles(
//             outputDirectoryPath, $"*{libraryFileNameExtension}", SearchOption.AllDirectories);
//         foreach (var outputFilePath in outputFilePaths)
//         {
//             var targetFilePath = outputFilePath.Replace(
//                     outputDirectoryPath, libraryOutputDirectoryPath, StringComparison.InvariantCulture);
//             var targetFileName = Path.GetFileName(targetFilePath);
//
//             if (runtimePlatform == RuntimeOperatingSystem.Windows)
//             {
//                 if (targetFileName.StartsWith("lib", StringComparison.InvariantCulture))
//                 {
//                     targetFileName = targetFileName[3..];
//                 }
//             }
//
//             var targetFileDirectoryPath = Path.GetDirectoryName(targetFilePath)!;
//             targetFilePath = Path.Combine(targetFileDirectoryPath, targetFileName);
//             if (!Directory.Exists(targetFileDirectoryPath))
//             {
//                 Directory.CreateDirectory(targetFileDirectoryPath);
//             }
//
//             if (File.Exists(targetFilePath))
//             {
//                 File.Delete(targetFilePath);
//             }
//
//             File.Copy(outputFilePath, targetFilePath);
//         }
//
//         Directory.Delete(outputDirectoryPath, true);
//         Directory.Delete($"{cMakeDirectoryPath}/cmake-build-release", true);
//
//         return true;
//     }
// }
