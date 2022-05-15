// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Contexts.ReadCodeC.Data.Model;
using Microsoft.Extensions.Logging;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore;
using static bottlenoselabs.clang;

public abstract class ExploreHandler<TNode> : ExploreHandler
    where TNode : CNode
{
    protected ExploreHandler(ILogger<ExploreHandler<TNode>> logger, bool logAlreadyExplored = true)
        : base(logger, logAlreadyExplored)
    {
    }
}

public abstract partial class ExploreHandler
{
    private readonly ILogger<ExploreHandler> _logger;
    private readonly Dictionary<string, CLocation> _visitedNames = new();
    private readonly bool _logAlreadyExplored;

    protected abstract ExploreKindCursors ExpectedCursors { get; }

    protected abstract ExploreKindTypes ExpectedTypes { get; }

    protected ExploreHandler(ILogger<ExploreHandler> logger, bool logAlreadyExplored = true)
    {
        _logger = logger;
        _logAlreadyExplored = logAlreadyExplored;
    }

    internal CNode ExploreInternal(ExploreContext context, ExploreInfoNode node)
    {
        LogExploring(node.Kind, node.Name, node.Location);
        var result = Explore(context, node);
        LogSuccess(node.Kind, node.Name, node.Location);
        return result;
    }

    internal bool CanVisitInternal(ExploreContext context, ExploreInfoNode node)
    {
        if (!IsExpectedCursor(node))
        {
            LogFailureUnexpectedCursor(node.Cursor.kind);
            return false;
        }

        if (!IsExpectedType(node))
        {
            LogFailureUnexpectedType(node.Type.kind);
            return false;
        }

        if (!IsAllowed(context, node.Name, node.Cursor))
        {
            return false;
        }

        if (IsAlreadyVisited(node.Name, out var firstLocation))
        {
            if (_logAlreadyExplored)
            {
                LogAlreadyVisited(node.Kind, node.Name, firstLocation);
            }

            return false;
        }

        if (!CanVisit(context, node.Name))
        {
            return false;
        }

        MarkAsVisited(node);
        return true;
    }

    internal bool IsBlocked(ExploreContext context, string name, CXCursor cursor)
    {
        if (!IsAllowed(context, name, cursor))
        {
            return true;
        }

        if (!CanVisit(context, name))
        {
            return true;
        }

        return false;
    }

    protected virtual bool CanVisit(ExploreContext context, string name)
    {
        return true;
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

    private bool IsAlreadyVisited(string name, out CLocation firstLocation)
    {
        var result = _visitedNames.TryGetValue(name, out firstLocation);
        return result;
    }

    private void MarkAsVisited(ExploreInfoNode info)
    {
        _visitedNames.Add(info.Name, info.Location);
    }

    private static bool IsAllowed(ExploreContext context, string name, CXCursor cursor)
    {
        if (!context.Options.IsEnabledSystemDeclarations)
        {
            var cursorLocation = clang_getCursorLocation(cursor);
            var isSystemCursor = clang_Location_isInSystemHeader(cursorLocation) > 0;
            if (isSystemCursor)
            {
                return false;
            }
        }

        if (!context.Options.IsEnabledAllowNamesWithPrefixedUnderscore)
        {
            var namesStartsWithUnderscore = name.StartsWith("_", StringComparison.InvariantCultureIgnoreCase);
            if (namesStartsWithUnderscore)
            {
                return false;
            }
        }

        return true;
    }

    public abstract CNode Explore(ExploreContext context, ExploreInfoNode info);

    [LoggerMessage(0, LogLevel.Error, "- Unexpected cursor kind '{Kind}'")]
    private partial void LogFailureUnexpectedCursor(CXCursorKind kind);

    [LoggerMessage(1, LogLevel.Error, "- Unexpected type kind '{Kind}'")]
    private partial void LogFailureUnexpectedType(CXTypeKind kind);

    [LoggerMessage(2, LogLevel.Debug, "- Exploring {Kind} '{Name}' ({Location})'")]
    public partial void LogExploring(CKind kind, string name, CLocation location);

    [LoggerMessage(3, LogLevel.Error, "- Already visited {Kind} '{Name}' ({Location})")]
    public partial void LogAlreadyVisited(CKind kind, string name, CLocation location);

    [LoggerMessage(4, LogLevel.Debug, "- Explored {Kind} '{Name}' ({Location})'")]
    public partial void LogSuccess(CKind kind, string name, CLocation location);
}
