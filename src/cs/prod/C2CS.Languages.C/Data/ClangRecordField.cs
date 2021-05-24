// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record ClangRecordField : ClangNode
    {
        public readonly string Name;
        public readonly ClangType Type;
        public readonly int Offset;
        public readonly int Padding;
        public readonly bool IsUnNamedFunctionPointer;

        internal ClangRecordField(
            string name,
            ClangCodeLocation codeLocation,
            ClangType type,
            int offset,
            bool isUnNamedFunctionPointer)
            : base(ClangNodeKind.RecordField, codeLocation)
        {
            Name = name;
            Type = type;
            Offset = offset;
            Padding = 0;
            IsUnNamedFunctionPointer = isUnNamedFunctionPointer;
        }

        internal ClangRecordField(
            ClangRecordField previous,
            int padding)
            : this(previous.Name, previous.CodeLocation, previous.Type, previous.Offset, previous.IsUnNamedFunctionPointer)
        {
            Padding = padding;
        }

        public override string ToString()
        {
            return $"RecordField '{Name}': {Type.Name} @ {CodeLocation.ToString()}";
        }
    }
}
