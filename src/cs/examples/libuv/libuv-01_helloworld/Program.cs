using System;
using System.Runtime.InteropServices;
using System.Threading;
using static uv;

internal static unsafe class Program
{
    public static uv_loop_t* Loop; 
    public static uv_idle_t* IdleHandle;
    
    private static void Main()
    {
        Loop = CreateLoop();
        IdleHandle = CreateIdleHandle(Loop);
        RunLoop();
        FreeResources();
    }
    
    private static uv_loop_t* CreateLoop()
    {
        var loop = (uv_loop_t*) Marshal.AllocHGlobal((int) uv_loop_size());
        var errorCode = uv_loop_init(loop);
        CheckErrorCode("uv_loop_init", errorCode);
        return loop;
    }
    
    private static uv_idle_t* CreateIdleHandle(uv_loop_t* loop)
    {
        var handle = (uv_idle_t*) Marshal.AllocHGlobal((int) uv_handle_size(uv_handle_type.UV_IDLE));

        var errorCode = uv_idle_init(loop, handle);
        CheckErrorCode("uv_idle_init", errorCode);

        errorCode = uv_idle_start(handle, new uv_idle_cb {Pointer = &OnIdle});
        CheckErrorCode("uv_idle_start", errorCode);

        return handle;
    }
    
    private static void RunLoop()
    {
        var errorCode = uv_run(Loop, uv_run_mode.UV_RUN_DEFAULT);
        CheckErrorCode("uv_run (UV_RUN_DEFAULT)", errorCode);
    }
    
    [UnmanagedCallersOnly]
    private static void OnIdle(uv_idle_t* handle)
    {
        // Check if we should gracefully exit.
        if (Console.KeyAvailable)
        {
            var consoleKey = Console.ReadKey();
            if (consoleKey.Key == ConsoleKey.X)
            {
                Console.WriteLine();
                // gracefully exit
                Environment.Exit(0);
            }
        }
        
        // REMOVE ME: Slow down the event loop for purposes of this demo by having the thread sleep
        Thread.Sleep(750);

        Console.WriteLine("Hello world!");
    }
    
    private static void FreeResources()
    {
        Marshal.FreeHGlobal((IntPtr) Loop);
        Marshal.FreeHGlobal((IntPtr) IdleHandle);
    }
}