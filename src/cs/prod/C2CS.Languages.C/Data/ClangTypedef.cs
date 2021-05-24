// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record ClangTypedef : ClangNode
    {
        public readonly string Name;
        public readonly ClangType Type;
        public readonly ClangType UnderlyingType;

        internal ClangTypedef(
            string name,
            ClangCodeLocation codeLocation,
            ClangType type,
            ClangType underlyingType)
            : base(ClangNodeKind.Typedef, codeLocation)
        {
            Name = name;
            Type = type;
            UnderlyingType = underlyingType;
        }

        public override string ToString()
        {
            return $"Record '{Name}': {UnderlyingType.Name} @ {CodeLocation.ToString()}";
        }
    }
}
