using System;
using System.Runtime.InteropServices;
using C2CS;
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
        }
        
        [StructLayout(LayoutKind.Sequential)] // Sequential necessary so C# is not allowed to reorganize struct
        public struct Velocity
        {
            public float X;
            public float Y;
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
                var p = ecs_term<Components.Position>(iterator, 1);
                var v = ecs_term<Components.Velocity>(iterator, 2);
    
                for (var i = 0; i < iterator->count; i++)
                {
                    p[i].X += v[i].X;
                    p[i].Y += v[i].X;

                    /* Print something to the console so we can see the system is being
                     * invoked */
                    var nameCString = ecs_get_name(iterator->world, iterator->entities[i]);
                    var nameString = Runtime.String(nameCString);
                    Console.WriteLine($"{nameString} moved to {{.x = {p[i].X}, .y = {p[i].Y}}}");
                }
            }
            
            public static readonly CString Name = "Move";
        }
    }

    public static class Entities
    {
        public static readonly CString MyEntity = "MyEntity";
    }

    private static int Main(string[] args)
    {
        /* Create the world, pass arguments for overriding the number of threads,fps
         * or for starting the admin dashboard (see flecs.h for details). */
        var world = ecs_init_w_args(args);

        /* Register components */
        var positionComponent = ecs_component_init<Components.Position>(world);
        var velocityComponent = ecs_component_init<Components.Velocity>(world);

        /* Define a system called Move that is executed every frame, and subscribes
         * for the 'Position' and 'Velocity' components */
        var systemDescriptor = new ecs_system_desc_t
        {
            entity = new ecs_entity_desc_t { name = Systems.Move.Name }
        };
        systemDescriptor.entity.add[0] = EcsOnUpdate;
        var queryFilterTerms = systemDescriptor.query.filter.terms;
        queryFilterTerms[0].id = positionComponent;
        queryFilterTerms[1].id = velocityComponent;
        systemDescriptor.callback.Pointer = &Systems.Move.Callback;
        ecs_system_init(world, &systemDescriptor);
        
        /* Create new entity, add the component to the entity */
        var entityDescriptor = new ecs_entity_desc_t
        {
            name = Entities.MyEntity
        };
        entityDescriptor.add[0] = positionComponent;
        entityDescriptor.add[1] = velocityComponent;
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
        ecs_set_id(world, entity, positionComponent, ref position);
        ecs_set_id(world, entity, velocityComponent, ref velocity);
        
        /* Set target FPS for main loop to 1 frame per second */
        ecs_set_target_fps(world, 1);
        
        Console.WriteLine("Application move_system is running, press CTRL-C to exit...");

        /* Run systems */
        while (ecs_progress(world, 0))
        {
        }

        /* Cleanup */
        return ecs_fini(world);
    }
}