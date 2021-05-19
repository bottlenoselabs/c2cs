using System;
using System.Runtime.InteropServices;
using static flecs;

internal static unsafe class Program
{
    private static class Components
    {
        [StructLayout(LayoutKind.Sequential)] // Sequential necessary so C# is not allowed to reorganize the struct
        public struct Position
        {
            public double X;
            public double Y;
            
            public static readonly sbyte* Name = NativeRuntime.MapCString(nameof(Position));
            public static readonly ulong Size = (ulong)Marshal.SizeOf<Position>();
            public const ulong Alignment = 8; // TODO: Find a way to get alignment of sequential struct.
        }
    }
    
    public static class Entities
    {
        public static readonly sbyte* MyEntity = NativeRuntime.MapCString("MyEntity");
    }

    private static int Main(string[] args)
    {
        LoadApi();
        
        /* Create the world, pass arguments for overriding the number of threads,fps
         * or for starting the admin dashboard (see flecs.h for details). */
        var argv = NativeRuntime.MapCStringArray(args);
        var world = ecs_init_w_args(args.Length, (sbyte**) argv);

        /* Register a component with the world. */
        var componentDescriptor = new ecs_component_desc_t
        {
            entity = {name = Components.Position.Name},
            size = Components.Position.Size,
            alignment = Components.Position.Alignment
        };
        var component = ecs_component_init(world, &componentDescriptor);

        /* Create a new empty entity  */
        var entityDescriptor = new ecs_entity_desc_t
        {
            name = Entities.MyEntity,
        };

        entityDescriptor.add[0] = component;
        // TODO: Switch to index property to get [] instead of () notation
        var entity = ecs_entity_init(world, &entityDescriptor);

        /* Set the Position component on the entity */
        var position = new Components.Position
        {
            X = 10,
            Y = 20
        };
        ecs_set_id(world, entity, component, Components.Position.Size, &position);

        /* Get the Position component */
        var p = (Components.Position*) ecs_get_w_id(world, entity, component);

        var nameCString = ecs_get_name(world, entity);
        var nameString = NativeRuntime.MapString(nameCString);
        
        Console.WriteLine($"Position of {nameString} is {p->X}, {p->Y}");

        /* Cleanup */
        return ecs_fini(world);
    }
}