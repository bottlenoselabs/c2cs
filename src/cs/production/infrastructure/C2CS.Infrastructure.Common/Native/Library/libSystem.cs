// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

// using System;
// using System.Runtime.InteropServices;
// using System.Security;
// using JetBrains.Annotations;
//
// #pragma warning disable SA1300
// // ReSharper disable IdentifierTypo
// // ReSharper disable InconsistentNaming
//
// namespace C2CS.Library
// {
//     [PublicAPI]
//     [SuppressUnmanagedCodeSecurity]
//     internal static class libSystem
//     {
//         private const string LibraryName = "libSystem";
//
//         [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
//         public static extern IntPtr dlopen(string fileName, int flags);
//
//         [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
//         public static extern IntPtr dlsym(IntPtr handle, string name);
//
//         [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall)]
//         public static extern int dlclose(IntPtr handle);
//
//         [DllImport(LibraryName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
//         public static extern string dlerror();
//     }
// }
