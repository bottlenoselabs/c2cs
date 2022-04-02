// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using C2CS.Foundation;
using C2CS.Foundation.Logging;
using Microsoft.Extensions.Logging;

namespace C2CS.Feature.ReadCodeC.Domain.ExploreCode;

internal static class Logging
{
    private static readonly Action<ILogger, Exception> ActionExploreCodeFailed =
        LoggerMessage.Define(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Failed."),
            "- Failed");

    private static readonly Action<ILogger, Exception> ActionExploreCodeSuccess =
        LoggerMessage.Define(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Success"),
            "- Success");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeTranslationUnit =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Translation unit"),
            "- Translation unit {FilePath}");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeMacro =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Macro"),
            "- Macro {Name}");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeVariable =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Variable"),
            "- Variable {Name}");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeFunction =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Function"),
            "- Function {Name}");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeEnum =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Enum"),
            "- Enum {Name}");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeRecord =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Record"),
            "- Record {Name}");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeTypedef =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Typedef"),
            "- Typedef {Name}");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeOpaqueType =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Opaque type"),
            "- Opaque type {Name}");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeFunctionPointer =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Function pointer"),
            "- Function pointer {Name}");

    private static readonly Action<ILogger, string, Exception> ActionExploreCodeType =
        LoggerMessage.Define<string>(
            LogLevel.Trace,
            LoggingEventRegistry.CreateEventIdentifier("Explore C header file: Type"),
            "- Type {TypeName}");

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

    public static void ExploreCodeMacro(this ILogger logger, string name)
    {
        ActionExploreCodeMacro(logger, name, null!);
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

    public static void ExploreCodeTypedef(this ILogger logger, string name)
    {
        ActionExploreCodeTypedef(logger, name, null!);
    }

    public static void ExploreCodeOpaqueType(this ILogger logger, string name)
    {
        ActionExploreCodeOpaqueType(logger, name, null!);
    }

    public static void ExploreCodeFunctionPointer(this ILogger logger, string name)
    {
        ActionExploreCodeFunctionPointer(logger, name, null!);
    }

    public static void ExploreCodeVisitType(this ILogger logger, string name)
    {
        ActionExploreCodeType(logger, name, null!);
    }
}
