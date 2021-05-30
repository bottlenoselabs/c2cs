// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using static libclang;

namespace C2CS.Languages.C
{
    // Known system types are ignored because they already built-in other languages. If they are not built-in they are
    //  supplemented by a thin runtime because they are so common.
    public class DiagnosticCSystemTypeIgnored : Diagnostic
    {
        public DiagnosticCSystemTypeIgnored(CXType type)
            : base("C2CS1000", DiagnosticSeverity.Information)
        {
            while (type.kind == CXTypeKind.CXType_Pointer)
            {
                type = clang_getPointeeType(type);
            }

            var cursor = clang_getTypeDeclaration(type);
            var typeName = type.GetName();
            var codeLocation = new CCodeLocation(cursor, true, false);
            Summary = $"The type '{typeName}' is a known system type that is not directly mapped. {codeLocation}";
        }
    }
}
