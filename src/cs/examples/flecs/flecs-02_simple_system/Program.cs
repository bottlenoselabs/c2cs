// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Runtime.InteropServices;
using C2CS;
using static flecs;

internal static unsafe class Program
{
    private static class Components
    {
        [StructLayout(LayoutKind.Sequential)] // Sequential necessary so C# is not allowed to reorganize the struct
        public struct Message
        {
            public CString Text;
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
                var msg = ecs_term<Components.Message>(iterator, 1);

                /* Iterate all the messages */
                for (var i = 0; i < msg.Length; i++)
                {
                    var text = Runtime.String(msg[i].Text);
                    Console.WriteLine(text);
                }
            }

            public static readonly CString Name = "PrintMessage";
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

        /* Define component */
        var component = ecs_component_init<Components.Message>(world);

        /* Define a system called PrintMessage that is executed every frame, and
         * subscribes for the 'Message' component */
        var systemDescriptor = new ecs_system_desc_t
        {
            entity = new ecs_entity_desc_t
            {
                name = Systems.PrintMessage.Name
            }
        };
        // systemDescriptor.entity.add[0] = EcsOnUpdate;
        systemDescriptor.query.filter.terms[0] = new ecs_term_t
        {
            id = component
        };
        systemDescriptor.callback.Pointer = &Systems.PrintMessage.Callback;
        ecs_system_init(world, &systemDescriptor);

        /* Create new entity, add the component to the entity */
        var entity = ecs_entity_init(world, Entities.MyEntity, component);

        /* Set the Position component on the entity */
        var message = new Components.Message
        {
            Text = "Hello Flecs!"
        };
        ecs_set_id(world, entity, component, ref message);

        /* Set target FPS for main loop to 1 frame per second */
        ecs_set_target_fps(world, 1);

        Console.WriteLine("Application simple_system is running, press CTRL-C to exit...");

        /* Run systems */
        while ( ecs_progress(world, 0) )
        {
        }

        /* Cleanup */
        return ecs_fini(world);
    }
}
