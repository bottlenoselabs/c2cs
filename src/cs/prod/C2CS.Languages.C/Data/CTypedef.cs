// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record CTypedef : CNode
    {
        public readonly string Name;
        public readonly CType Type;
        public readonly CType UnderlyingType;

        internal CTypedef(
            string name,
            CCodeLocation codeLocation,
            CType type,
            CType underlyingType)
            : base(CNodeKind.Typedef, codeLocation)
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
