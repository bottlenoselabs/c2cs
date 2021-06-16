// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using static libclang;

namespace C2CS.UseCases.BindgenCSharp
{
    // Overriding a type as an opaque type is necessary in some situations to hide implementation or operating system
    //  specific details.
    public class DiagnosticTypeOpaqueOverriden : Diagnostic
    {
        public DiagnosticTypeOpaqueOverriden(CXType type)
            : base(DiagnosticSeverity.Information)
        {
            while (type.kind == CXTypeKind.CXType_Pointer)
            {
                type = clang_getPointeeType(type);
            }

            var typeName = type.Name();
            Summary = $"The type '{typeName}' was overriden to an opaque type.";
        }
    }
}
