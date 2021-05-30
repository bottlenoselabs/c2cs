// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record CEnumValue : CNode
    {
        public readonly string Name;
        public readonly long Value;

        internal CEnumValue(
            string name,
            CCodeLocation codeLocation,
            long value)
            : base(CNodeKind.EnumValue, codeLocation)
        {
            Name = name;
            Value = value;
        }

        public override string ToString()
        {
            return $"EnumValue '{Name}' = {Value} @ {CodeLocation.ToString()}";
        }
    }
}
