
//-------------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the following tool:
//        https://github.com/lithiumtoast/c2cs
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ReSharper disable All
//-------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;

#nullable enable

public static unsafe partial class helloworld
{
    private const string LibraryName = "helloworld";
    private static IntPtr _libraryHandle;

    public static void LoadApi(string? libraryFilePath = null)
    {
        UnloadApi();
        if (libraryFilePath == null)
        {
            var libraryFileNamePrefix = Runtime.LibraryFileNamePrefix;
            var libraryFileNameExtension = Runtime.LibraryFileNameExtension;
            libraryFilePath = $@"{libraryFileNamePrefix}{LibraryName}{libraryFileNameExtension}";
        }
        _libraryHandle = Runtime.LibraryLoad(libraryFilePath);
        if (_libraryHandle == IntPtr.Zero)
            throw new Exception($"Failed to load library: {libraryFilePath}");
        LoadExports();
    }

    public static void UnloadApi()
    {
        if (_libraryHandle == IntPtr.Zero)
            return;
        UnloadExports();
        Runtime.LibraryUnload(_libraryHandle);
    }

    private static void LoadExports()
    {

    }

    private static void UnloadExports()
    {

    }

    // Function @ library.h:6
    [DllImport(LibraryName, EntryPoint = "hello_world", CallingConvention = CallingConvention.Cdecl)]
    public static extern void hello_world();
}