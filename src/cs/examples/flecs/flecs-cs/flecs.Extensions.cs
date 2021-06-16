using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

[PublicAPI]
[SuppressMessage("ReSharper", "InconsistentNaming", Justification = "C style.")]
[SuppressMessage("ReSharper", "IdentifierTypo", Justification = "C style.")]
public static unsafe partial class flecs
{
    public static ecs_world_t* ecs_init_w_args(ReadOnlySpan<string> args)
    {
        var argv = Runtime.AllocateCStringArray(args);
        var world = ecs_init_w_args(args.Length, argv);
        Runtime.FreeCStrings(argv, args.Length);
        return world;
    }

    public static ecs_entity_t ecs_entity_init(
        ecs_world_t* world, CString name, Span<ecs_id_t> componentIds)
    {
        var entityDescriptor = new ecs_entity_desc_t
        {
            name = name
        };

        for (var index = 0; index < componentIds.Length; index++)
        {
            var componentId = componentIds[index];
            entityDescriptor.add[index] = componentId;
        }

        return ecs_entity_init(world, &entityDescriptor);
    }

    public static ecs_entity_t ecs_entity_init(ecs_world_t* world, CString name, ecs_id_t componentId)
    {
        var entityDescriptor = new ecs_entity_desc_t
        {
            name = name
        };

        entityDescriptor.add[0] = componentId;

        return ecs_entity_init(world, &entityDescriptor);
    }
    
    public static ecs_entity_t ecs_component_init<TComponent>(ecs_world_t* world)
        where TComponent : unmanaged
    {
        var componentType = typeof(TComponent);
        var componentName = componentType.Name;
        var componentNameC = Runtime.AllocateCString(componentName);
        var structLayoutAttribute = componentType.StructLayoutAttribute;
        CheckStructLayout(structLayoutAttribute);
        var structAlignment = structLayoutAttribute!.Pack;
        var structSize = Unsafe.SizeOf<TComponent>();

        var componentDescriptor = new ecs_component_desc_t
        {
            entity = {name = componentNameC},
            size = (ulong) structSize,
            alignment = (ulong) structAlignment
        };

        return ecs_component_init(world, &componentDescriptor);
    }

    public static ecs_entity_t ecs_set_id<T>(ecs_world_t* world, ecs_entity_t entity, ecs_id_t componentId, ref T component)
        where T : unmanaged
    {
        var componentType = typeof(T);
        var structLayoutAttribute = componentType.StructLayoutAttribute;
        CheckStructLayout(structLayoutAttribute);
        var structSize = Unsafe.SizeOf<T>();
        var pointer = Unsafe.AsPointer(ref component);
        return ecs_set_id(world, entity, componentId, (ulong) structSize, pointer);
    }

    public static ref readonly T ecs_get_id<T>(ecs_world_t* world, ecs_entity_t entity, ecs_id_t id)
        where T : unmanaged
    {
        var pointer = ecs_get_id(world, entity, id);
        return ref Unsafe.AsRef<T>(pointer);
    }

    public static Span<T> ecs_term<T>(ecs_iter_t* it, int index)
        where T : unmanaged
    {
        var structSize = Unsafe.SizeOf<T>();
        var pointer = ecs_term_w_size(it, (ulong) structSize, index);
        return new Span<T>(pointer, it->count);
    }

    private static void CheckStructLayout(StructLayoutAttribute? structLayoutAttribute)
    {
        if (structLayoutAttribute == null || structLayoutAttribute.Value == LayoutKind.Auto)
        {
            throw new FlecsException(
                "Component must have a StructLayout attribute with LayoutKind sequential or explicit. This is to ensure that the struct fields are not reorganized.");
        }
    }
}