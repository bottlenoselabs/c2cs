using System;
using lithiumtoast.NativeTools;
using static flecs;

internal static class Program
{
    public unsafe static class Components
    {
        public struct PositionComponent
        {
            public double X;
            public double Y;
            
            public static readonly sbyte* Id = Native.MapCString("Position");
            public const ulong Size = 8;
            public const ulong Alignment = 8;
        }
    }

    public unsafe static class Entities
    {
        public static sbyte* MyEntity = Native.MapCString("MyEntity");
    }

    private static unsafe int Main(string[] args)
    {
        /* Create the world, pass arguments for overriding the number of threads,fps
         * or for starting the admin dashboard (see flecs.h for details). */
        var argv = Native.MapCStringArray(args);
        var world = ecs_init_w_args(args.Length, (sbyte**) argv);

        /* Register a component with the world. */
        var component = ecs_new_component(world, default, Components.PositionComponent.Id, Components.PositionComponent.Size, Components.PositionComponent.Alignment);

        /* Create a new empty entity  */
        var entity = ecs_new_entity(world, default, (sbyte*) 0, (sbyte*) 0);
        // ecs_set_ptr_w_id(world, Entities.MyEntity, 6, )
        //
        // /* Set the Position component on the entity */
        // var position = new Components.PositionComponent
        // {
        //     X = 10,
        //     Y = 20
        // };
        // var id = new ecs_id_t { Data = component.Data };
        // ecs_set_ptr_w_id(world, entity, id, Components.PositionComponent.Size, &position);

        // /* Get the Position component */
        // var p = (Components.PositionComponent*) ecs_get_w_id(world, component, id);
        //
        // var nameCString = ecs_get_name(world, entity);
        // var nameString = Native.MapString(nameCString);
        //
        // Console.WriteLine($"Position of {nameString} is {p->X}, {p->Y}\n");

        /* Cleanup */
        return ecs_fini(world);
    }
}