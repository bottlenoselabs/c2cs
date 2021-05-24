using System;
using System.Runtime.InteropServices;
using static uv;

internal static class Program
{
    private static unsafe void Main()
    {
        var loop = (uv_loop_t*) Marshal.AllocHGlobal(Marshal.SizeOf<uv_loop_t>());
        var errorCode = uv_loop_init(loop);
        if (errorCode < 0)
        {
            var cStringErrorName = uv_err_name(errorCode);
            var stringErrorName = NativeRuntime.GetString(cStringErrorName);
            Console.WriteLine(stringErrorName);
        }

        Console.WriteLine("Now quitting.\n");
        errorCode = uv_run(loop, uv_run_mode.UV_RUN_DEFAULT);
        if (errorCode < 0)
        {
            var cStringErrorName = uv_err_name(errorCode);
            var stringErrorName = NativeRuntime.GetString(cStringErrorName);
            Console.WriteLine(stringErrorName);
        }

        errorCode = uv_loop_close(loop);
        if (errorCode < 0)
        {
            var cStringErrorName = uv_err_name(errorCode);
            var stringErrorName = NativeRuntime.GetString(cStringErrorName);
            Console.WriteLine(stringErrorName);
        }
        
        Marshal.FreeHGlobal((IntPtr) loop);
    }
}