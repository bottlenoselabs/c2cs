// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Data.C.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Explore.Handlers;

[UsedImplicitly]
public sealed class FunctionPointerExplorer : ExploreHandler<CFunctionPointer>
{
    public FunctionPointerExplorer(ILogger<FunctionPointerExplorer> logger)
        : base(logger, false)
    {
    }

    // NOTE: Function pointer visiting by name.
    //  (1) A typedef can be an alias to a function pointer. Typedefs are declarations, declarations always have a
    //      cursor. The name of the function pointer is the name of the typedef. Thus, there can only ever be one
    //      function pointer with that name.
    //  (2) A function pointer can be inlined either to a: struct field, function result, function parameter,
    //      function pointer result, function pointer parameter. Take a moment to think about the last two ones; yes,
    //      an inlined function pointer can be nested inside another function pointer's result or parameters.
    //      This can happen recursively. The name of an inlined function pointer is taken from it's type signature.
    //      This means that it is possible for a function pointer by type to visited multiple times from different
    //      locations (struct field, function result, function parameter, function pointer result, function pointer
    //      parameter).

    protected override ExploreKindCursors ExpectedCursors => ExploreKindCursors.Any;

    protected override ExploreKindTypes ExpectedTypes { get; } = ExploreKindTypes.Either(
        CXTypeKind.CXType_FunctionProto, CXTypeKind.CXType_FunctionNoProto);

    public override CFunctionPointer Explore(ExploreContext context, ExploreInfoNode info)
    {
        var functionPointer = FunctionPointer(context, info);
        return functionPointer;
    }

    private static CFunctionPointer FunctionPointer(ExploreContext context, ExploreInfoNode info)
    {
        var typeInfo = context.VisitType(info.Type, info, kindHint: CKind.FunctionPointer)!;
        var returnTypeInfo = FunctionPointerReturnType(context, info);
        var parameters = FunctionPointerParameters(context, info);

        var result = new CFunctionPointer
        {
            Name = info.Name,
            Location = info.Location,
            TypeInfo = typeInfo,
            ReturnTypeInfo = returnTypeInfo,
            Parameters = parameters
        };
        return result;
    }

    private static CTypeInfo FunctionPointerReturnType(ExploreContext context, ExploreInfoNode info)
    {
        var returnType = clang_getResultType(info.Type);
        var returnTypeInfo = context.VisitType(returnType, info)!;
        return returnTypeInfo;
    }

    private static ImmutableArray<CFunctionPointerParameter> FunctionPointerParameters(
        ExploreContext context,
        ExploreInfoNode info)
    {
        var builder = ImmutableArray.CreateBuilder<CFunctionPointerParameter>();

        var count = clang_getNumArgTypes(info.Type);
        for (uint i = 0; i < count; i++)
        {
            var parameterType = clang_getArgType(info.Type, i);
            var functionPointerParameter = FunctionPointerParameter(context, parameterType, info);
            builder.Add(functionPointerParameter);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private static CFunctionPointerParameter FunctionPointerParameter(
        ExploreContext context,
        CXType parameterType,
        ExploreInfoNode parentInfo)
    {
        var parameterTypeInfo = context.VisitType(parameterType, parentInfo)!;
        var result = new CFunctionPointerParameter
        {
            Name = string.Empty,
            TypeInfo = parameterTypeInfo
        };
        return result;
    }
}
