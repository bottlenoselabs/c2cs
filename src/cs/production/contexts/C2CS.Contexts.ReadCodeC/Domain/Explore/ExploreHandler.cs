// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Contexts.ReadCodeC.Data.Model;
using Microsoft.Extensions.Logging;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore;
using static bottlenoselabs.clang;

public abstract class ExploreHandler<TNode> : ExploreHandler
    where TNode : CNode
{
    protected ExploreHandler(ILogger<ExploreHandler<TNode>> logger)
        : base(logger)
    {
    }
}

public abstract partial class ExploreHandler
{
    private readonly ILogger<ExploreHandler> _logger;
    private readonly Dictionary<string, CLocation> _visitedNames = new();

    protected abstract ExploreKindCursors ExpectedCursors { get; }

    protected abstract ExploreKindTypes ExpectedTypes { get; }

    protected ExploreHandler(ILogger<ExploreHandler> logger)
    {
        _logger = logger;
    }

    public CNode? Visit(ExploreContext context, ExploreInfoNode info)
    {
        if (!IsExpectedCursor(info))
        {
            LogFailureUnexpectedCursor(info.Cursor.kind);
            return null;
        }

        if (!IsExpectedType(info))
        {
            LogFailureUnexpectedType(info.Type.kind);
            return null;
        }

        if (IsAlreadyVisited(info.Name, info.Location, out var firstLocation))
        {
            LogAlreadyExplored(info.Kind, info.Name, firstLocation);
            return null;
        }

        // if (context.Options.OpaqueTypesNames.Contains(name))
        // {
        //     OnFoundOpaqueType(context, name, node.Type, location);
        //     return;
        // }

        LogExploring(info.Kind, info.Name, info.Location);
        var result = Explore(context, info);
        if (result == null)
        {
            LogIgnored(info.Kind, info.Name, info.Location);
            return null;
        }

        LogSuccess(info.Kind, info.Name, info.Location);
        return result;
    }

    private bool IsExpectedCursor(ExploreInfoNode info)
    {
        if (ExpectedCursors.Matches(info.Cursor.kind))
        {
            return true;
        }

        return false;
    }

    private bool IsExpectedType(ExploreInfoNode info)
    {
        var typeKind = info.Type.kind;
        if (ExpectedTypes.Matches(typeKind))
        {
            return true;
        }

        return false;
    }

    private bool IsAlreadyVisited(string name, CLocation location, out CLocation firstLocation)
    {
        var result = _visitedNames.TryGetValue(name, out firstLocation);
        if (!result)
        {
            _visitedNames.Add(name, location);
        }

        return result;
    }

    public abstract CNode? Explore(ExploreContext context, ExploreInfoNode info);

    [LoggerMessage(0, LogLevel.Error, "- Unexpected cursor kind '{Kind}'")]
    private partial void LogFailureUnexpectedCursor(CXCursorKind kind);

    [LoggerMessage(1, LogLevel.Error, "- Unexpected type kind '{Kind}'")]
    private partial void LogFailureUnexpectedType(CXTypeKind kind);

    [LoggerMessage(2, LogLevel.Error, "- Unexpected parent '{ParentName}'")]
    public partial void LogFailureUnexpectedParent(string parentName);

    [LoggerMessage(3, LogLevel.Debug, "- Exploring {Kind} '{Name}' ({Location})'")]
    public partial void LogExploring(CKind kind, string name, CLocation location);

    [LoggerMessage(4, LogLevel.Error, "- Already explored {Kind} '{Name}' ({Location})")]
    public partial void LogAlreadyExplored(CKind kind, string name, CLocation location);

    [LoggerMessage(5, LogLevel.Debug, "- Ignored {Kind} '{Name}' ({Location})")]
    public partial void LogIgnored(CKind kind, string name, CLocation location);

    [LoggerMessage(6, LogLevel.Debug, "- Explored {Kind} '{Name}' ({Location})'")]
    public partial void LogSuccess(CKind kind, string name, CLocation location);
}
