// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using C2CS.Contexts.ReadCodeC.Data.Model;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using static bottlenoselabs.clang;

namespace C2CS.Contexts.ReadCodeC.Domain.Explore.Handlers;

[UsedImplicitly]
public sealed class MacroExploreHandler : ExploreHandler<CMacroObject>
{
    protected override ExploreKindCursors ExpectedCursors { get; } = ExploreKindCursors.Is(CXCursorKind.CXCursor_MacroDefinition);

    protected override ExploreKindTypes ExpectedTypes => ExploreKindTypes.Any;

    public MacroExploreHandler(ILogger<MacroExploreHandler> logger)
        : base(logger)
    {
    }

    public override CMacroObject? Explore(ExploreContext context, ExploreInfoNode info)
    {
        if (info.Parent != null)
        {
            LogFailureUnexpectedParent(info.Parent.Name);
            return null;
        }

        if (!IsAllowed(context, info))
        {
            return null;
        }

        var macroObject = MacroObject(info);
        return macroObject;
    }

    private static bool IsAllowed(ExploreContext context, ExploreInfoNode info)
    {
        // Function-like macros currently not implemented
        // https://github.com/lithiumtoast/c2cs/issues/35
        if (clang_Cursor_isMacroFunctionLike(info.Cursor) > 0)
        {
            return false;
        }

        if (!context.Options.IsEnabledMacroObjects)
        {
            return false;
        }

        var name = info.Name;
        // Assume that macros with a name which starts with an underscore are not supposed to be exposed in the public API
        if (name.StartsWith("_", StringComparison.InvariantCulture))
        {
            return false;
        }

        // Assume that macro ending with "API_DECL" are not interesting for bindgen
        if (name.EndsWith("API_DECL", StringComparison.InvariantCulture))
        {
            return false;
        }

        // Assume that macros starting with names of the C helper macros are not interesting for bindgen
        if (name.StartsWith("PINVOKE_TARGET_", StringComparison.InvariantCulture))
        {
            return false;
        }

        // if (name == "PINVOKE_TARGET_PLATFORM_NAME")
        // {
        //     var actualPlatformName = tokens.Length != 1 ? string.Empty : tokens[0].Replace("\"", string.Empty, StringComparison.InvariantCulture);
        //     var actualPlatform = new TargetPlatform(actualPlatformName);
        //     var expectedPlatform = context.TargetPlatform;
        //     if (actualPlatform != expectedPlatform)
        //     {
        //         var diagnostic = new PlatformMismatchDiagnostic(actualPlatform, expectedPlatform);
        //         context.Diagnostics.Add(diagnostic);
        //     }
        //
        //     return false;
        // }

        return true;
    }

    private CMacroObject? MacroObject(ExploreInfoNode info)
    {
        // clang doesn't have a thing where we can easily get a value of a macro
        // we need to:
        //  1. get the text range of the cursor
        //  2. get the tokens over said text range
        //  3. go through the tokens to parse the value
        // this means we get to do token parsing ourselves, yay!
        // NOTE: The first token will always be the name of the macro
        var translationUnit = clang_Cursor_getTranslationUnit(info.Cursor);
        string[] tokens;
        unsafe
        {
            var range = clang_getCursorExtent(info.Cursor);
            var tokensC = (CXToken*)0;
            uint tokensCount = 0;

            clang_tokenize(translationUnit, range, &tokensC, &tokensCount);

            var macroIsFlag = tokensCount is 0 or 1;
            if (macroIsFlag)
            {
                clang_disposeTokens(translationUnit, tokensC, tokensCount);
                return null;
            }

            tokens = new string[tokensCount - 1];
            for (var i = 1; i < (int)tokensCount; i++)
            {
                var spelling = clang_getTokenSpelling(translationUnit, tokensC[i]);
                var cString = (string)clang_getCString(spelling);

                // CLANG BUG?: https://github.com/FNA-XNA/FAudio/blob/b84599a5e6d7811b02329709a166a337de158c5e/include/FAPOBase.h#L90
                if (cString.StartsWith('\\'))
                {
                    cString = cString.TrimStart('\\');
                }

                tokens[i - 1] = cString.Trim();
            }

            clang_disposeTokens(translationUnit, tokensC, tokensCount);
        }

        // Remove redundant parenthesis
        if (tokens.Length > 2)
        {
            if (tokens[0] == "(" && tokens[^1] == ")")
            {
                var newTokens = new string[tokens.Length - 2];
                Array.Copy(tokens, 1, newTokens, 0, tokens.Length - 2);
                tokens = newTokens;
            }
        }

        var result = new CMacroObject
        {
            Name = info.Name,
            Tokens = tokens.ToImmutableArray(),
            Location = info.Location
        };

        return result;
    }
}
