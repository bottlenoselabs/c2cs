// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record ClangOpaqueType : ClangNode
    {
        public readonly string Name;

        public ClangOpaqueType(
            string name,
            ClangCodeLocation codeLocation)
            : base(ClangNodeKind.OpaqueType, codeLocation)
        {
            Name = name;
        }

        public override string ToString()
        {
            return $"OpaqueType '{Name}' @ {CodeLocation.ToString()}";
        }
    }
}
