// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ReadCodeC.Data.Model;
using C2CS.Foundation;
using C2CS.Foundation.Diagnostics;

namespace C2CS.Feature.ReadCodeC.Domain.ExploreCode;

public sealed class ExplorerContext
{
    public DiagnosticsSink Diagnostics { get; }

    public TargetPlatform TargetPlatform { get; }

    public ImmutableArray<string> UserIncludeDirectories { get; }

    public ExploreOptions Options { get; }

    internal readonly HashSet<string> VisitedFunctionNames = new();
    internal readonly HashSet<string> VisitedFunctionPointerNames = new();
    internal readonly HashSet<string> VisitedPrimitiveNames = new();
    internal readonly HashSet<string> VisitedPointerNames = new();
    internal readonly HashSet<string> VisitedRecordNames = new();
    internal readonly HashSet<string> VisitedTypedefNames = new();
    internal readonly HashSet<string> VisitedArrayNames = new();
    internal readonly HashSet<string> VisitedOpaqueTypeNames = new();

    internal readonly Dictionary<string, CFunctionPointer> FunctionPointers = new();
    internal readonly Dictionary<string, CFunction> Functions = new();
    internal readonly Dictionary<string, CMacroDefinition> MacroObjects = new();
    internal readonly Dictionary<string, CEnum> Enums = new();
    internal readonly Dictionary<string, CRecord> Records = new();
    internal readonly Dictionary<string, COpaqueType> OpaqueDataTypes = new();
    internal readonly Dictionary<string, CTypedef> Typedefs = new();
    internal readonly Dictionary<string, CVariable> Variables = new();

    internal readonly ArrayDeque<ExplorerNode> FrontierMacros = new();
    internal readonly ArrayDeque<ExplorerNode> FrontierApi = new();
    internal readonly ArrayDeque<ExplorerNode> FrontierTypes = new();

    internal readonly HashSet<string> MacroFunctionLikeNames = new();
    internal readonly HashSet<string> SystemIgnoredTypeNames = DefaultSystemIgnoredTypeNames();
    internal readonly Dictionary<string, bool> ValidTypeNames = new();

    public ExplorerContext(
        DiagnosticsSink diagnostics,
        TargetPlatform targetPlatform,
        ExploreOptions options,
        ImmutableArray<string> userIncludeDirectories)
    {
        Diagnostics = diagnostics;
        TargetPlatform = targetPlatform;
        Options = options;
        UserIncludeDirectories = userIncludeDirectories;
    }

    private static HashSet<string> DefaultSystemIgnoredTypeNames()
    {
        return new HashSet<string>
        {
            "FILE",
            "DIR",
            "size_t",
            "ssize_t",
            "int8_t",
            "uint8_t",
            "int16_t",
            "uint16_t",
            "int32_t",
            "uint32_t",
            "int64_t",
            "uint64_t",
            "uintptr_t",
            "intptr_t",
            "va_list"
        };
    }
}
