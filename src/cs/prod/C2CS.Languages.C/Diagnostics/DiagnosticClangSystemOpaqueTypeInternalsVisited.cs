// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using static libclang;

namespace C2CS.Languages.C
{
    // When internals of a system opaque type that are visited it usually leads to an issue where operating system
    //  specific or otherwise implementation specific details are revealed. This can be an issue for bindings which want
    //  to be neutral or be independent of such specific details.
    public class DiagnosticClangSystemOpaqueTypeInternalsVisited : Diagnostic
    {
        public DiagnosticClangSystemOpaqueTypeInternalsVisited(CXType type)
            : base(DiagnosticSeverity.Warning)
        {
            var cursor = clang_getTypeDeclaration(type);
            var codeLocation = new ClangCodeLocation(cursor, true, false);
            var typeName = type.GetName();
            Summary = $"The internals of the system opaque type {typeName} were visited. {codeLocation}";
        }
    }
}
