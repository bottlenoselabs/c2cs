using System;
using System.Runtime.InteropServices;
using System.Threading;
using static uv;

internal static unsafe class Program
{
    public static uv_loop_t* Loop; // Main event loop
    public static uv_timer_t* TimerHandle; // callback after specific time BEFORE I/O; always runs before idle handles
    public static uv_idle_t* IdleHandle; // callback BEFORE I/O; when an idle handle is active (non-stopped), there is timeout for polling; this means that an idle callbacks are useful for giving feedback to the user that the application is not frozen
    public static uv_prepare_t* PrepareHandle; // callback BEFORE I/O; always runs after idle handles
    public static uv_check_t* CheckHandle; // callback AFTER I/O;

    private static void Main()
    {
        LoadApi();
        
        Console.WriteLine("Tick example: demo of event loop callback flow, useful as an example of simplistic base application with `libuv`. Press `x` to gracefully exit. Press `CTRL-C` to hard exit.");

        Loop = CreateLoop();
        TimerHandle = CreateTimerHandle(Loop);
        IdleHandle = CreateIdleHandle(Loop);
        PrepareHandle = CreatePrepareHandle(Loop);
        CheckHandle = CreateCheckHandle(Loop);
        RunLoop();
        FreeResources();
    }

    private static uv_loop_t* CreateLoop()
    {
        var loop = (uv_loop_t*) Marshal.AllocHGlobal((int) uv_loop_size());
        var errorCode = uv_loop_init(loop);
        PrintErrorCode("uv_loop_init", errorCode);
        return loop;
    }
    
    private static uv_timer_t* CreateTimerHandle(uv_loop_t* loop)
    {
        var handle = (uv_timer_t*) Marshal.AllocHGlobal(Marshal.SizeOf<uv_timer_t>());
        
        var errorCode = uv_timer_init(loop, handle);
        PrintErrorCode("uv_timer_init", errorCode);

        var elapsedTimeSpan = TimeSpan.FromSeconds(1);
        errorCode = uv_timer_start(handle, new uv_timer_cb {Pointer = &OnTimer}, 0, (ulong) elapsedTimeSpan.TotalMilliseconds);
        PrintErrorCode("uv_timer_start", errorCode);

        return handle;
    }

    private static uv_idle_t* CreateIdleHandle(uv_loop_t* loop)
    {
        var handle = (uv_idle_t*) Marshal.AllocHGlobal(Marshal.SizeOf<uv_idle_t>());

        var errorCode = uv_idle_init(loop, handle);
        PrintErrorCode("uv_idle_init", errorCode);

        errorCode = uv_idle_start(handle, new uv_idle_cb {Pointer = &OnIdle});
        PrintErrorCode("uv_idle_start", errorCode);

        return handle;
    }
  
    private static uv_prepare_t* CreatePrepareHandle(uv_loop_t* loop)
    {
        var handle = (uv_prepare_t*) Marshal.AllocHGlobal(Marshal.SizeOf<uv_prepare_t>());

        var errorCode = uv_prepare_init(loop, handle);
        PrintErrorCode("uv_prepare_init", errorCode);

        errorCode = uv_prepare_start(handle, new uv_prepare_cb {Pointer = &OnPrepare});
        PrintErrorCode("uv_prepare_start", errorCode);

        return handle;
    }
    
    private static uv_check_t* CreateCheckHandle(uv_loop_t* loop)
    {
        var handle = (uv_check_t*) Marshal.AllocHGlobal(Marshal.SizeOf<uv_check_t>());
        
        var errorCode = uv_check_init(loop, handle);
        PrintErrorCode("uv_check_init", errorCode);

        errorCode = uv_check_start(handle, new uv_check_cb {Pointer = &OnCheck});
        PrintErrorCode("uv_check_start", errorCode);

        return handle;
    }

    private static void RunLoop()
    {
        var errorCode = uv_run(Loop, uv_run_mode.UV_RUN_DEFAULT);
        PrintErrorCode("uv_run (UV_RUN_DEFAULT)", errorCode);
    }

    private static void FreeResources()
    {
        Marshal.FreeHGlobal((IntPtr) Loop);
        Marshal.FreeHGlobal((IntPtr) TimerHandle);
        Marshal.FreeHGlobal((IntPtr) IdleHandle);
        Marshal.FreeHGlobal((IntPtr) PrepareHandle);
        Marshal.FreeHGlobal((IntPtr) CheckHandle);
    }

    private static void Exit()
    {
        // walk the list of handles with a callback for each
        uv_walk(Loop, new uv_walk_cb {Pointer = &TryCloseHandle}, default);
        PrintErrorCode("uv_walk", 0);
        
        // run the loop one more time so handle close callbacks are executed
        var errorCode = uv_run(Loop, uv_run_mode.UV_RUN_NOWAIT);
        PrintErrorCode("uv_run (uv_run_mode.UV_RUN_NOWAIT)", errorCode);
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
                Exit();
            }
        }
        
        // REMOVE ME: Slow down the event loop for purposes of this demo by having the thread sleep
        Thread.Sleep(750);

        Console.WriteLine("Tick: idle; called before I/O.");
    }
    
    [UnmanagedCallersOnly]
    private static void OnPrepare(uv_prepare_t* handle)
    {
        Console.WriteLine("Tick: prepare; called before I/O.");
    }
    
    [UnmanagedCallersOnly]
    private static void OnCheck(uv_check_t* handle)
    {
        Console.WriteLine("Tick: check; called after I/O.");
    }
    
    [UnmanagedCallersOnly]
    private static void OnTimer(uv_timer_t* handle)
    {
        Console.WriteLine("Tick: timer elapsed; called before I/O.");
    }

    [UnmanagedCallersOnly]
    private static void TryCloseHandle(uv_handle_t* handle, void* param)
    {
        var isClosing = uv_is_closing(handle);
        if (isClosing != 0)
        {
            return;
        }
        
        var handleType = uv_handle_get_type(handle);
        var cStringHandleTypeName = uv_handle_type_name(handleType);
        var handleTypeName = Runtime.AllocateString(cStringHandleTypeName);
        uv_close(handle, new uv_close_cb {Pointer = &OnHandleClosed});
        Console.WriteLine($"Handle of type '{handleTypeName}' is closing.");
    }
    
    [UnmanagedCallersOnly]
    private static void OnHandleClosed(uv_handle_t* handle)
    {
        var handleType = uv_handle_get_type(handle);
        var cStringHandleTypeName = uv_handle_type_name(handleType);
        var handleTypeName = Runtime.AllocateString(cStringHandleTypeName);
        Console.WriteLine($"Handle of type '{handleTypeName}' is closed.");
    }
    
    private static void PrintErrorCode(string functionName, int errorCode)
    {
        var status = errorCode >= 0 ? "success" : "failure";

        if (errorCode == 0)
        {
            Console.WriteLine($"{functionName}: {status}");  
        }
        else
        {
            var name = GetErrorCodeName(errorCode);
            var description = GetErrorCodeDescription(errorCode);
            Console.WriteLine($"{functionName}: {status} {name} {description}");     
        }
    }

    private static string GetErrorCodeName(int errorCode)
    {
        var cString = uv_err_name(errorCode);
        var result = Runtime.AllocateString(cString);
        return result;
    }

    private static string GetErrorCodeDescription(int errorCode)
    {
        var buffer = stackalloc byte[512];
        var cString = uv_strerror_r(errorCode, buffer, 512);
        var result = Runtime.AllocateString(cString);
        return result;
    }
}