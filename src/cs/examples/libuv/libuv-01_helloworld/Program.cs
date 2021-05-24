using System;
using System.Runtime.InteropServices;
using static uv;

internal static class Program
{
    private static unsafe void Main()
    {
        var loop = (uv_loop_t*) Marshal.AllocHGlobal((int) uv_loop_size());
        var errorCode = uv_loop_init(loop);
        if (errorCode < 0)
        {
            PrintErrorCode(errorCode);
        }

        Console.WriteLine("Hello world.");
        errorCode = uv_run(loop, uv_run_mode.UV_RUN_DEFAULT);
        if (errorCode < 0)
        {
            PrintErrorCode(errorCode);
        }

        errorCode = uv_loop_close(loop);
        if (errorCode < 0)
        {
            PrintErrorCode(errorCode);
        }
        
        Marshal.FreeHGlobal((IntPtr) loop);
    }
    
    private static unsafe void PrintErrorCode(int errorCode)
    {
        var cStringErrorName = uv_err_name(errorCode);
        var stringErrorName = NativeRuntime.GetString(cStringErrorName);

        var errorDescriptionBuffer = stackalloc byte[512];
        var cStringErrorDescription = uv_strerror_r(errorCode, errorDescriptionBuffer, 512);
        var stringErrorDescription = NativeRuntime.GetString(cStringErrorDescription);
        
        Console.WriteLine($"Error {stringErrorName}: {stringErrorDescription}");
    }
}