// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Data.C.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.ReadCodeC.Domain.Explore.Handlers;

[UsedImplicitly]
public sealed class FunctionExplorer : ExploreNodeHandler<CFunction>
{
    public FunctionExplorer(ILogger<FunctionExplorer> logger)
        : base(logger, false)
    {
        // NOTE: Function visiting by name.
        //  When a header file contains the declaration of the function, it may later be implemented in the same header
        //  file or another header file. When this happens there will be two function declaration cursors with the same
        //  name even if they have the same type signature (result type and parameter types).
        //  For this reason, do not log if already visited.
    }

    protected override ExploreKindCursors ExpectedCursors { get; } =
        ExploreKindCursors.Is(CXCursorKind.CXCursor_FunctionDecl);

    protected override ExploreKindTypes ExpectedTypes { get; } = ExploreKindTypes.Either(
        CXTypeKind.CXType_FunctionProto, CXTypeKind.CXType_FunctionNoProto);

    protected override bool CanVisit(ExploreContext context, string name, ExploreInfoNode? parentInfo)
    {
        var result = context.Reader.CanVisitFunction(name);
        return result;
    }

    public override CFunction Explore(ExploreContext context, ExploreInfoNode info)
    {
        var function = Function(context, info);
        return function;
    }

    private CFunction Function(ExploreContext context, ExploreInfoNode info)
    {
        var callingConvention = FunctionCallingConvention(info.Type);
        var returnTypeInfo = FunctionReturnType(context, info);
        var parameters = FunctionParameters(context, info);

        var result = new CFunction
        {
            Name = info.Name,
            Location = info.Location,
            CallingConvention = callingConvention,
            ReturnTypeInfo = returnTypeInfo,
            Parameters = parameters
        };
        return result;
    }

    private static CFunctionCallingConvention FunctionCallingConvention(CXType type)
    {
        var callingConvention = clang_getFunctionTypeCallingConv(type);
        var result = callingConvention switch
        {
            CXCallingConv.CXCallingConv_C => CFunctionCallingConvention.Cdecl,
            CXCallingConv.CXCallingConv_X86StdCall => CFunctionCallingConvention.StdCall,
            CXCallingConv.CXCallingConv_X86FastCall => CFunctionCallingConvention.FastCall,
            _ => CFunctionCallingConvention.Unknown
        };

        return result;
    }

    private static CTypeInfo FunctionReturnType(
        ExploreContext context, ExploreInfoNode info)
    {
        var resultType = clang_getCursorResultType(info.Cursor);
        return context.VisitType(resultType, info)!;
    }

    private ImmutableArray<CFunctionParameter> FunctionParameters(
        ExploreContext context,
        ExploreInfoNode info)
    {
        var builder = ImmutableArray.CreateBuilder<CFunctionParameter>();

        var count = clang_Cursor_getNumArguments(info.Cursor);
        for (uint i = 0; i < count; i++)
        {
            var parameterCursor = clang_Cursor_getArgument(info.Cursor, i);
            var functionParameter = FunctionParameter(context, parameterCursor, info);
            if (functionParameter == null)
            {
                continue;
            }

            builder.Add(functionParameter);
        }

        var result = builder.ToImmutable();
        return result;
    }

    private static CFunctionParameter? FunctionParameter(
        ExploreContext context,
        CXCursor parameterCursor,
        ExploreInfoNode parentInfo)
    {
        var name = context.CursorName(parameterCursor);
        var parameterType = clang_getCursorType(parameterCursor);

        var parameterTypeInfo = context.VisitType(parameterType, parentInfo);
        if (parameterTypeInfo == null)
        {
            return null;
        }

        var typeInfo = parameterTypeInfo;
        while (typeInfo != null)
        {
            var headerFilesBlocked = context.ExploreOptions.HeaderFilesBlocked;
            if (headerFilesBlocked.Contains(typeInfo.Location.FileName))
            {
                return null;
            }

            typeInfo = typeInfo.InnerTypeInfo;
        }

        var functionExternParameter = new CFunctionParameter
        {
            Name = name,
            Location = parameterTypeInfo.Location,
            TypeInfo = parameterTypeInfo
        };
        return functionExternParameter;
    }
}
