using System;
using System.Runtime.InteropServices;
using static flecs;

internal static unsafe class Program
{
    private static class Components
    {
        [StructLayout(LayoutKind.Sequential)] // Sequential necessary so C# is not allowed to reorganize the struct
        public struct Message
        {
            public byte* Text;
            
            public static readonly byte* Name = NativeRuntime.GetCString(nameof(Message));
            public static readonly ulong Size = (ulong)Marshal.SizeOf<Message>();
            public const ulong Alignment = 8;
        }
    }

    private static class Systems
    {
        public static class PrintMessage
        {
            [UnmanagedCallersOnly]
            public static void Callback(ecs_iter_t* iterator)
            {
                /* Get a pointer to the array of the first column in the system. The order
                 * of columns is the same as the one provided in the system signature. */
                var msg = (Components.Message*)ecs_term_w_size(iterator, Components.Message.Size, 1);
    
                /* Iterate all the messages */
                for (var i = 0; i < iterator->count; i++)
                {
                    var text = NativeRuntime.GetString(msg[i].Text);
                    Console.WriteLine(text);
                }
            }
            
            public static readonly byte* Name = NativeRuntime.GetCString("PrintMessage");
        }
    }
    
    public static class Entities
    {
        public static readonly byte* MyEntity = NativeRuntime.GetCString("MyEntity");
    }

    private static int Main(string[] args)
    {
        LoadApi();
        
        /* Create the world, pass arguments for overriding the number of threads,fps
         * or for starting the admin dashboard (see flecs.h for details). */
        var argv = NativeRuntime.GetCStringArray(args);
        var world = ecs_init_w_args(args.Length, (byte**) argv);

        /* Define component */
        var componentDescriptor = new ecs_component_desc_t
        {
            entity = {name = Components.Message.Name},
            size = Components.Message.Size,
            alignment = Components.Message.Alignment
        };
        var component = ecs_component_init(world, &componentDescriptor);

        /* Define a system called PrintMessage that is executed every frame, and
         * subscribes for the 'Message' component */
        var systemDescriptor = new ecs_system_desc_t
        {
            entity = new ecs_entity_desc_t
            {
                name = Systems.PrintMessage.Name
            }
        };
        systemDescriptor.entity.add[0] = EcsOnUpdate;
        systemDescriptor.query.filter.terms[0] = new ecs_term_t
        {
            id = component
        };
        systemDescriptor.callback.Pointer = &Systems.PrintMessage.Callback;
        ecs_system_init(world, &systemDescriptor);
        
        /* Create new entity, add the component to the entity */
        var entityDescriptor = new ecs_entity_desc_t
        {
            name = Entities.MyEntity
        };
        entityDescriptor.add[0] = component;
        var entity = ecs_entity_init(world, &entityDescriptor);

        /* Set the Position component on the entity */
        var message = new Components.Message
        {
            Text = NativeRuntime.GetCString("Hello Flecs!")
        };
        ecs_set_id(world, entity, component, Components.Message.Size, &message);
        
        /* Set target FPS for main loop to 1 frame per second */
        ecs_set_target_fps(world, 1);
        
        Console.WriteLine("Application simple_system is running, press CTRL-C to exit...");

        /* Run systems */
        while ( ecs_progress(world, 0));

        /* Cleanup */
        return ecs_fini(world);
    }
}