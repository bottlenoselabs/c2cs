using System;
using System.Runtime.InteropServices;
using static flecs;

internal static unsafe class Program
{
    /* Component types */
    private static class Components
    {
        [StructLayout(LayoutKind.Sequential)] // Sequential necessary so C# is not allowed to reorganize struct
        public struct Position
        {
            public float X;
            public float Y;
            
            public static readonly sbyte* Name = NativeTools.MapCString(nameof(Position));
            public static readonly ulong Size = (ulong)Marshal.SizeOf<Position>();
            public const ulong Alignment = 8;
        }
        
        [StructLayout(LayoutKind.Sequential)] // Sequential necessary so C# is not allowed to reorganize struct
        public struct Velocity
        {
            public float X;
            public float Y;
            
            public static readonly sbyte* Name = NativeTools.MapCString(nameof(Velocity));
            public static readonly ulong Size = (ulong)Marshal.SizeOf<Velocity>();
            public const ulong Alignment = 8;
        }
    }
    
    private static class Systems
    {
        public static class Move
        {
            /* Implement a simple move system */
            [UnmanagedCallersOnly]
            public static void Callback(ecs_iter_t* iterator)
            {
                /* Get the two columns from the system signature */
                var p = (Components.Position*)ecs_term_w_size(iterator, Components.Position.Size, 1);
                var v = (Components.Velocity*)ecs_term_w_size(iterator, Components.Velocity.Size, 2);
    
                for (var i = 0; i < iterator->count; i++)
                {
                    p[i].X += v[i].X;
                    p[i].Y += v[i].X;

                    /* Print something to the console so we can see the system is being
                     * invoked */
                    var nameCString = ecs_get_name(iterator->world, iterator->entities[i]);
                    var nameString = NativeTools.MapString(nameCString);
                    Console.WriteLine($"{nameString} moved to {{.x = {p[i].X}, .y = {p[i].Y}}}");
                }
            }
            
            public static readonly sbyte* Name = NativeTools.MapCString("PrintMessage");
        }
    }

    public static class Entities
    {
        public static readonly sbyte* MyEntity = NativeTools.MapCString("MyEntity");
    }

    private static int Main(string[] args)
    {
        LoadApi();
        
        /* Create the world, pass arguments for overriding the number of threads,fps
         * or for starting the admin dashboard (see flecs.h for details). */
        var argv = NativeTools.MapCStringArray(args);
        var world = ecs_init_w_args(args.Length, (sbyte**) argv);

        /* Register components */
        var positionComponentDescriptor = new ecs_component_desc_t
        {
            entity = {name = Components.Position.Name},
            size = Components.Position.Size,
            alignment = Components.Position.Alignment
        };
        var positionComponent = ecs_component_init(world, &positionComponentDescriptor);
        var positionComponentId = *(ecs_id_t*)(&positionComponent); // TODO: Remove this nasty type cast
        
        var velocityComponentDescriptor = new ecs_component_desc_t
        {
            entity = {name = Components.Velocity.Name},
            size = Components.Velocity.Size,
            alignment = Components.Velocity.Alignment
        };
        var velocityComponent = ecs_component_init(world, &velocityComponentDescriptor);
        var velocityComponentId = *(ecs_id_t*)(&velocityComponent); // TODO: Remove this nasty type cast

        /* Define a system called Move that is executed every frame, and subscribes
         * for the 'Position' and 'Velocity' components */
        var systemDescriptor = new ecs_system_desc_t
        {
            entity = new ecs_entity_desc_t { name = Systems.Move.Name }
        };
        var ecsOnUpdate = EcsOnUpdate; // TODO: Remove this temp variable
        systemDescriptor.entity.add(0) = *(ecs_id_t*)(&ecsOnUpdate); // TODO: Remove this nasty type cast
        systemDescriptor.query.filter.terms(0).id = positionComponentId;
        systemDescriptor.query.filter.terms(1).id = velocityComponentId;
        systemDescriptor.callback.Pointer = &Systems.Move.Callback; // TODO: Add an implicit cast operator
        ecs_system_init(world, &systemDescriptor);
        
        /* Create new entity, add the component to the entity */
        var entityDescriptor = new ecs_entity_desc_t
        {
            name = Entities.MyEntity
        };
        entityDescriptor.add(0) = positionComponentId; // TODO: Switch to index property to get [] instead of () notation
        entityDescriptor.add(1) = velocityComponentId;
        var entity = ecs_entity_init(world, &entityDescriptor);

        /* Initialize values for the entity */
        var position = new Components.Position
        {
            X = 0,
            Y = 0
        };
        var velocity = new Components.Velocity
        {
            X = 1,
            Y = 1
        };
        ecs_set_id(world, entity, positionComponentId, Components.Position.Size, &position);
        ecs_set_id(world, entity, velocityComponentId, Components.Velocity.Size, &velocity);
        
        /* Set target FPS for main loop to 1 frame per second */
        ecs_set_target_fps(world, 1);
        
        Console.WriteLine("Application move_system is running, press CTRL-C to exit...");

        /* Run systems */
        while ( ecs_progress(world, 0));

        /* Cleanup */
        return ecs_fini(world);
    }
}