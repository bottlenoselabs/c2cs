using System.Runtime.InteropServices;
using C2CS;
using static sokol;

internal static unsafe class Program
{
    private static Rgba32F _color;

    private static void Main()
    {
        var desc = default(sapp_desc);
        desc.init_cb.Pointer = &Initialize;
        desc.frame_cb.Pointer = &Frame;
        desc.cleanup_cb.Pointer = &Cleanup;
        desc.width = 400;
        desc.height = 300;
        desc.gl_force_gles2 = true;
        desc.window_title = Runtime.CString("Clear (sokol app)");
        
        sapp_run(&desc);
    }   
    
    [UnmanagedCallersOnly]
    private static void Initialize()
    {
        var desc = default(sg_desc);
        desc.context = sapp_sgcontext();
        sg_setup(&desc);
    }
    
    [UnmanagedCallersOnly]
    private static void Frame()
    {
        var g = _color.G + 0.01f;
        _color.G = g > 1.0f ? 0.0f : g;

        var passAction = default(sg_pass_action);
        passAction.colors[0].action = sg_action.SG_ACTION_CLEAR;
        passAction.colors[0].value = _color;
        sg_begin_default_pass(&passAction, sapp_width(), sapp_height());
        sg_end_pass();
        sg_commit();
    }
    
    [UnmanagedCallersOnly]
    private static void Cleanup()
    {
        sg_shutdown();
    }
}