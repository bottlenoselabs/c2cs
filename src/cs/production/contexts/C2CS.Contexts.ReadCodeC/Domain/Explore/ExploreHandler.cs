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

    public CNode Visit(ExploreContext context, ExploreInfoNode info)
    {
        LogExploring(info.Kind, info.Name, info.Location);
        var result = Explore(context, info);
        LogSuccess(info.Kind, info.Name, info.Location);
        return result;
    }

    internal bool CanVisitInternal(ExploreContext context, ExploreInfoNode info)
    {
        if (!IsExpectedCursor(info))
        {
            LogFailureUnexpectedCursor(info.Cursor.kind);
            return false;
        }

        if (!IsExpectedType(info))
        {
            LogFailureUnexpectedType(info.Type.kind);
            return false;
        }

        if (!IsAllowed(context, info))
        {
            return false;
        }

        if (IsAlreadyVisited(info.Name, info.Location, out var firstLocation))
        {
            if (_logAlreadyExplored)
            {
                LogAlreadyExplored(info.Kind, info.Name, firstLocation);
            }

            return false;
        }

        if (!CanVisit(context, info))
        {
            LogIgnored(info.Kind, info.Name, info.Location);
            return false;
        }

        return true;
    }

    protected abstract bool CanVisit(ExploreContext context, ExploreInfoNode info);

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

    protected bool IsAlreadyVisited(string name, CLocation location, out CLocation firstLocation)
    {
        var result = _visitedNames.TryGetValue(name, out firstLocation);
        if (!result)
        {
            _visitedNames.Add(name, location);
        }

        return result;
    }

    private static bool IsAllowed(ExploreContext context, ExploreInfoNode info)
    {
        if (!context.Options.IsEnabledAllowNamesWithPrefixedUnderscore)
        {
            return !info.Name.StartsWith("_", StringComparison.InvariantCultureIgnoreCase);
        }

        return true;
    }

    public abstract CNode Explore(ExploreContext context, ExploreInfoNode info);

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
