// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record ClangOpaqueType : ClangNode
    {
        public readonly string Name;
        public readonly int SizeOf;
        public readonly int AlignOf;

        public ClangOpaqueType(
            string name,
            ClangCodeLocation codeLocation,
            int sizeOf,
            int alignOf)
            : base(ClangNodeKind.OpaqueType, codeLocation)
        {
            Name = name;
            SizeOf = sizeOf;
            AlignOf = alignOf;
        }

        public override string ToString()
        {
            return $"OpaqueType '{Name}' @ {CodeLocation.ToString()}";
        }
    }
}
