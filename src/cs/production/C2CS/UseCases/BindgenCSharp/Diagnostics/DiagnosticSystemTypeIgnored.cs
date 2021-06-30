// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using static clang;

namespace C2CS.UseCases.BindgenCSharp
{
    // Known system types are ignored because they already built-in other languages. If they are not built-in they are
    //  supplemented by a thin runtime because they are so common.
    public class DiagnosticSystemTypeIgnored : Diagnostic
    {
        public DiagnosticSystemTypeIgnored(CXType type)
            : base(DiagnosticSeverity.Information)
        {
            while (type.kind == CXTypeKind.CXType_Pointer)
            {
                type = clang_getPointeeType(type);
            }

            var typeName = type.Name();
            Summary = $"The type '{typeName}' is a known system type that is not directly mapped.";
        }
    }
}
