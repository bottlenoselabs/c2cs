using System;
using System.Runtime.InteropServices;
using static flecs;

internal static class Program
{
    private static unsafe class Components
    {
        public struct PositionComponent
        {
            public double X;
            public double Y;
            
            public static readonly sbyte* Name = NativeTools.MapCString("Position");
            public static ulong Size = (ulong)Marshal.SizeOf<PositionComponent>();
            public const ulong Alignment = 8;
        }
    }
    
    public static unsafe class Entities
    {
        public static sbyte* MyEntity = NativeTools.MapCString("MyEntity");
    }

    private static unsafe int Main(string[] args)
    {
        /* Create the world, pass arguments for overriding the number of threads,fps
         * or for starting the admin dashboard (see flecs.h for details). */
        var argv = NativeTools.MapCStringArray(args);
        var world = ecs_init_w_args(args.Length, (sbyte**) argv);

        /* Register a component with the world. */
        var componentDescriptor = default(ecs_component_desc_t);
        componentDescriptor.entity.name = Components.PositionComponent.Name;
        componentDescriptor.size = (ulong) Marshal.SizeOf<Components.PositionComponent>();
        componentDescriptor.alignment = 8;
        var component = ecs_component_init(world, &componentDescriptor);
        var componentId = *(ecs_id_t*)(&component); // TODO: Remove this nasty type cast

        /* Create a new empty entity  */
        var entityDescriptor = default(ecs_entity_desc_t);
        entityDescriptor.name = Entities.MyEntity;
        entityDescriptor.add(0) = componentId; // TODO: Switch to index property to get [] instead of () notation
        var entity = ecs_entity_init(world, &entityDescriptor);

        /* Set the Position component on the entity */
        var position = new Components.PositionComponent
        {
            X = 10,
            Y = 20
        };
        ecs_set_ptr_w_id(world, entity, componentId, Components.PositionComponent.Size, &position);

        /* Get the Position component */
        var p = (Components.PositionComponent*) ecs_get_w_id(world, entity, componentId);

        var nameCString = ecs_get_name(world, entity);
        var nameString = NativeTools.MapString(nameCString);
        
        Console.WriteLine($"Position of {nameString} is {p->X}, {p->Y}\n");

        /* Cleanup */
        return ecs_fini(world);
    }
}