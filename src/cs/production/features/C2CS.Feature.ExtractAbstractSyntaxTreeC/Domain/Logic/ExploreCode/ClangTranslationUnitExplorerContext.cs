// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Domain.Logic.ExploreCode;

public sealed class ClangTranslationUnitExplorerContext
{
    public DiagnosticsSink Diagnostics { get; }

    public ImmutableArray<string> IncludeDirectories { get; }

    public ImmutableArray<string> IgnoredFiles { get; }

    public ImmutableArray<string> OpaqueTypesNames { get; }

    public ImmutableArray<string> FunctionNamesWhitelist { get; }

    public RuntimePlatform TargetPlatform { get; }

    internal readonly List<CEnum> Enums = new();
    internal readonly ArrayDeque<ClangTranslationUnitExplorerNode> FrontierGeneral = new();
    internal readonly ArrayDeque<ClangTranslationUnitExplorerNode> FrontierMacros = new();
    internal readonly List<CFunctionPointer> FunctionPointers = new();
    internal readonly List<CFunction> Functions = new();
    internal readonly HashSet<string> MacroFunctionLikeNames = new();
    internal readonly List<CMacroDefinition> MacroObjects = new();
    internal readonly HashSet<string> Names = new();
    internal readonly List<COpaqueType> OpaqueDataTypes = new();
    internal readonly List<CRecord> Records = new();
    internal readonly HashSet<string> SystemIgnoredTypeNames = DefaultSystemIgnoredTypeNames();
    internal readonly List<CTypedef> Typedefs = new();
    internal readonly List<CType> Types = new();
    internal readonly Dictionary<string, CType> TypesByName = new();
    internal readonly Dictionary<string, bool> ValidTypeNames = new();
    internal readonly List<CVariable> Variables = new();

    public ClangTranslationUnitExplorerContext(
        DiagnosticsSink diagnostics,
        ImmutableArray<string> includeDirectories,
        ImmutableArray<string> ignoredFiles,
        ImmutableArray<string> opaqueTypeNames,
        ImmutableArray<string> functionNamesWhitelist,
        RuntimePlatform targetPlatform)
    {
        Diagnostics = diagnostics;
        IncludeDirectories = includeDirectories;
        IgnoredFiles = ignoredFiles;
        OpaqueTypesNames = opaqueTypeNames;
        FunctionNamesWhitelist = functionNamesWhitelist;
        TargetPlatform = targetPlatform;
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
