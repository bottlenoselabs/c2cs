// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

// using System;
// using System.Runtime.InteropServices;
// using C2CS.Library;
//
// namespace C2CS;
//
// public static class LibraryHelper
// {
//     public static IntPtr LoadLibrary(string name)
//     {
//         var operatingSystem = Native.OperatingSystem;
//         if (operatingSystem == NativeOperatingSystem.Linux)
//         {
//             return libdl.dlopen(name, 0x101); // RTLD_GLOBAL | RTLD_LAZY
//         }
//         else if (operatingSystem == NativeOperatingSystem.Windows)
//         {
//             return Kernel32.LoadLibrary(name);
//         }
//         if (IsLinux) return libdl.dlopen(name, 0x101); // RTLD_GLOBAL | RTLD_LAZY
//         if (IsWindows) return Kernel32.LoadLibrary(name);
//         if (IsDarwin) return libSystem.dlopen(name, 0x101); // RTLD_GLOBAL | RTLD_LAZY
//         return IntPtr.Zero;
//     }
//
//     public static void FreeLibrary(IntPtr handle)
//     {
//         if (IsLinux) libdl.dlclose(handle);
//         if (IsWindows) Kernel32.FreeLibrary(handle);
//         if (IsDarwin) libSystem.dlclose(handle);
//     }
//
//     public static IntPtr GetLibraryFunctionPointer(IntPtr handle, string functionName)
//     {
//         if (IsLinux) return libdl.dlsym(handle, functionName);
//         if (IsWindows) return Kernel32.GetProcAddress(handle, functionName);
//         if (IsDarwin) return libSystem.dlsym(handle, functionName);
//         return IntPtr.Zero;
//     }
//
//     public static T GetLibraryFunction<T>(IntPtr handle, string functionName)
//     {
//         var functionHandle = GetLibraryFunctionPointer(handle, functionName);
//         return Marshal.GetDelegateForFunctionPointer<T>(functionHandle);
//     }
// }
