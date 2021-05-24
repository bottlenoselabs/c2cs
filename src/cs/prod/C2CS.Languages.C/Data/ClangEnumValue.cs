// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record ClangEnumValue : ClangNode
    {
        public readonly string Name;
        public readonly long Value;

        internal ClangEnumValue(
            string name,
            ClangCodeLocation codeLocation,
            long value)
            : base(ClangNodeKind.EnumValue, codeLocation)
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
