// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using Microsoft.Extensions.Logging;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.ExploreCode;

internal static class Logging
{
    private const string Name = "Explore C header file";

    private static readonly Action<ILogger, Exception> ActionExploreCodeFailed =
        LoggerMessage.Define(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier(Name + ": Failed."),
            "- " + Name + ": Failed.");

    private static readonly Action<ILogger, Exception> ActionExploreCodeSuccess =
        LoggerMessage.Define(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier(Name + ": Success."),
            "- " + Name + ": Success.");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeTranslationUnit =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier(Name + ": Translation unit."),
            "- " + Name + ": Translation unit {FilePath}.");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeVariable =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier(Name + ": Variable."),
            "- " + Name + ": Variable {Name}.");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeFunction =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier(Name + ": Function."),
            "- " + Name + ": Function {Name}.");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeEnum =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier(Name + ": Enum."),
            "- " + Name + ": Enum {Name}.");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeRecord =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier(Name + ": Enum."),
            "- " + Name + ": Record {Name}.");

    public static void ExploreCodeFailed(this ILogger logger, Exception exception)
    {
        ActionExploreCodeFailed(logger, exception);
    }

    public static void ExploreCodeSuccess(this ILogger logger)
    {
        ActionExploreCodeSuccess(logger, null!);
    }

    public static void ExploreCodeTranslationUnit(this ILogger logger, string filePath)
    {
        ActionExploreCodeTranslationUnit(logger, filePath, null!);
    }

    public static void ExploreCodeVariable(this ILogger logger, string name)
    {
        ActionExploreCodeVariable(logger, name, null!);
    }

    public static void ExploreCodeFunction(this ILogger logger, string name)
    {
        ActionExploreCodeFunction(logger, name, null!);
    }

    public static void ExploreCodeEnum(this ILogger logger, string name)
    {
        ActionExploreCodeEnum(logger, name, null!);
    }

    public static void ExploreCodeRecord(this ILogger logger, string name)
    {
        ActionExploreCodeRecord(logger, name, null!);
    }
}
