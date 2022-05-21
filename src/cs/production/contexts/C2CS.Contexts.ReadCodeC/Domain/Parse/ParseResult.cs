// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using bottlenoselabs;
using C2CS.Contexts.ReadCodeC.Data.Model;

namespace C2CS.Contexts.ReadCodeC.Domain.Parse;

public class ParseResult
{
    public clang.CXTranslationUnit TranslationUnit { get; init; }

    public ImmutableArray<CMacroObject> MacroObjects { get; init; } = ImmutableArray<CMacroObject>.Empty;

    public ImmutableDictionary<string, string> LinkedPaths { get; init; } = ImmutableDictionary<string, string>.Empty;
}
