using System;
using System.Runtime.InteropServices;
using static flecs;

internal static unsafe class Program
{
    private static class Components
    {
        [StructLayout(LayoutKind.Sequential)] // Sequential necessary so C# compiler is not allowed to reorganize struct
        public struct Message
        {
            public sbyte* Text;
            
            public static readonly sbyte* Name = NativeTools.MapCString(nameof(Message));
            public static readonly ulong Size = (ulong)Marshal.SizeOf<Message>();
            public const ulong Alignment = 8;
        }
    }
    
    public static class Entities
    {
        public static readonly sbyte* MyEntity = NativeTools.MapCString("MyEntity");
    }
    
    [UnmanagedCallersOnly]
    private static void PrintMessage(ecs_iter_t* iterator)
    {
        /* Get a pointer to the array of the first column in the system. The order
         * of columns is the same as the one provided in the system signature. */
        var msg = (Components.Message*)ecs_term_w_size(iterator, Components.Message.Size, 1);
    
        /* Iterate all the messages */
        for (var i = 0; i < iterator->count; i ++)
        {
            var text = NativeTools.MapString(msg[i].Text);
            Console.WriteLine(text);
        }
    }

    private static int Main(string[] args)
    {
        LoadApi();
        
        /* Create the world, pass arguments for overriding the number of threads,fps
         * or for starting the admin dashboard (see flecs.h for details). */
        var argv = NativeTools.MapCStringArray(args);
        var world = ecs_init_w_args(args.Length, (sbyte**) argv);

        /* Define component */
        var componentDescriptor = new ecs_component_desc_t
        {
            entity = {name = Components.Message.Name},
            size = Components.Message.Size,
            alignment = Components.Message.Alignment
        };
        var component = ecs_component_init(world, &componentDescriptor);
        var componentId = *(ecs_id_t*)(&component); // TODO: Remove this nasty type cast

        /* Define a system called PrintMessage that is executed every frame, and
         * subscribes for the 'Message' component */
        var systemName = NativeTools.MapCString("PrintMessage");
        var systemSignature = NativeTools.MapCString("Message");
        var systemCallback = new ecs_iter_action_t { Pointer = &PrintMessage }; // TODO: Add an implicit cast operator
        var onUpdate = EcsOnUpdate;
        ecs_new_system(world, default, systemName, onUpdate, systemSignature, systemCallback);
        
        /* Create new entity, add the component to the entity */
        var entityDescriptor = new ecs_entity_desc_t
        {
            name = Entities.MyEntity
        };
        entityDescriptor.add(0) = componentId; // TODO: Switch to index property to get [] instead of () notation
        var entity = ecs_entity_init(world, &entityDescriptor);

        /* Set the Position component on the entity */
        var message = new Components.Message
        {
            Text = NativeTools.MapCString("Hello Flecs!")
        };
        ecs_set_ptr_w_id(world, entity, componentId, Components.Message.Size, &message);
        
        /* Set target FPS for main loop to 1 frame per second */
        ecs_set_target_fps(world, 1);
        
        Console.WriteLine("Application simple_system is running, press CTRL-C to exit...");

        /* Run systems */
        while ( ecs_progress(world, 0));

        /* Cleanup */
        return ecs_fini(world);
    }
}