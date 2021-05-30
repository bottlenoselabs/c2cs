// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using static libclang;

namespace C2CS.Languages.C
{
    // Overriding a type as an opaque type is necessary in some situations to hide implementation or operating system
    //  specific details.
    public class DiagnosticCTypeOpaqueOverriden : Diagnostic
    {
        public DiagnosticCTypeOpaqueOverriden(CXType type)
            : base("C2CS1002", DiagnosticSeverity.Information)
        {
            while (type.kind == CXTypeKind.CXType_Pointer)
            {
                type = clang_getPointeeType(type);
            }

            var typeName = type.GetName();
            var cursor = clang_getTypeDeclaration(type);
            var codeLocation = new CCodeLocation(cursor, true, false);
            Summary = $"The type '{typeName}' was overriden to an opaque type. {codeLocation}";
        }
    }
}
