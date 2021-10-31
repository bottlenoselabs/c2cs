// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using static clang;

namespace C2CS.UseCases.Macros;

public class UseCase : UseCase<Request, Response>
{
    protected override void Execute(Request request, Response response)
    {
        Validate(request);
        TotalSteps(3);

        var translationUnit = Step(
            "Parse C code from disk",
            request.InputFilePath,
            request.AutomaticallyFindSoftwareDevelopmentKit,
            request.IncludeDirectories,
            Parse);

        Step(
            "Print macro names",
            translationUnit,
            request.IncludeDirectories,
            PrintMacroNames);
    }

    private static void Validate(Request request)
    {
        if (!File.Exists(request.InputFilePath))
        {
            throw new UseCaseException($"File does not exist: `{request.InputFilePath}`.");
        }
    }

    private static CXTranslationUnit Parse(
        string inputFilePath,
        bool automaticallyFindSoftwareDevelopmentKit,
        ImmutableArray<string> includeDirectories)
    {
        var clangArgs = ClangArgumentsBuilder.Build(
            automaticallyFindSoftwareDevelopmentKit,
            includeDirectories,
            ImmutableArray<string>.Empty,
            null,
            ImmutableArray<string>.Empty);
        return ClangTranslationUnitParser.Parse(inputFilePath, clangArgs);
    }

    private static void PrintMacroNames(
        CXTranslationUnit translationUnit,
        ImmutableArray<string> includeDirectories)
    {
        var translationUnitCursor = clang_getTranslationUnitCursor(translationUnit);
        var macros = translationUnitCursor.GetDescendents(IsObjectLikeMacro);
        var macroNames = new HashSet<string>();

        foreach (var macro in macros)
        {
            var name = macro.Name();
            var location = macro.FileLocation();
            if (string.IsNullOrEmpty(location.FilePath))
            {
                continue;
            }

            var isInInclude = false;
            foreach (var includeDirectory in includeDirectories)
            {
                if (location.FilePath.StartsWith(includeDirectory, StringComparison.InvariantCulture))
                {
                    isInInclude = true;
                    break;
                }
            }

            if (!isInInclude)
            {
                continue;
            }

            if (macroNames.Contains(name))
            {
                continue;
            }

            macroNames.Add(name);
        }

        foreach (var name in macroNames)
        {
            // TODO: Follow up on the macro initialization to see if it's a constant value
            Console.WriteLine(name);
        }

        static bool IsObjectLikeMacro(CXCursor cursor, CXCursor parent)
        {
            var name = cursor.Name();
            var kind = clang_getCursorKind(cursor);

            if (kind != CXCursorKind.CXCursor_MacroDefinition)
            {
                return false;
            }

            if (clang_Cursor_isMacroFunctionLike(cursor) != 0)
            {
                return false;
            }

            if (clang_Cursor_isMacroBuiltin(cursor) != 0)
            {
                return false;
            }

            if (cursor.IsSystem())
            {
                return false;
            }

            return true;
        }
    }
}
