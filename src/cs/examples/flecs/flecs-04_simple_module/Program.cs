// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using static flecs;

internal static unsafe class Program
{
    private static int Main(string[] args)
    {
        /* Create the world, pass arguments for overriding the number of threads,fps
         * or for starting the admin dashboard (see flecs.h for details). */
        var world = ecs_init_w_args(args);

        // var name = NativeRuntime.GetCString("SimpleModule");
        // void* moduleHandle = default;
        // ulong moduleSize = default;
        //
        // ecs_import(world, new ecs_module_action_t {Pointer = &SimpleModule.LoadCallback}, name, default, default);

        // /* Create new entity, add the component to the entity */
        // var entityDescriptor = new ecs_entity_desc_t
        // {
        //     name = Entities.MyEntity
        // };
        // entityDescriptor.add[0] = positionComponent;
        // entityDescriptor.add[1] = velocityComponent;
        // var entity = ecs_entity_init(world, &entityDescriptor);
        //
        // /* Initialize values for the entity */
        // var position = new Components.Position
        // {
        //     X = 0,
        //     Y = 0
        // };
        // var velocity = new Components.Velocity
        // {
        //     X = 1,
        //     Y = 1
        // };
        // ecs_set_id(world, entity, positionComponent, Components.Position.Size, &position);
        // ecs_set_id(world, entity, velocityComponent, Components.Velocity.Size, &velocity);
        //
        // /* Set target FPS for main loop to 1 frame per second */
        // ecs_set_target_fps(world, 1);
        //
        // Console.WriteLine("Application move_system is running, press CTRL-C to exit...");

        /* Run systems */
        while (ecs_progress(world, 0))
        {
        }

        /* Cleanup */
        return ecs_fini(world);
    }
}
