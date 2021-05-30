// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record CRecordField : CNode
    {
        public readonly string Name;
        public readonly CType Type;
        public readonly int Offset;
        public readonly int Padding;
        public readonly bool IsUnNamedFunctionPointer;

        internal CRecordField(
            string name,
            CCodeLocation codeLocation,
            CType type,
            int offset,
            bool isUnNamedFunctionPointer)
            : base(CNodeKind.RecordField, codeLocation)
        {
            Name = name;
            Type = type;
            Offset = offset;
            Padding = 0;
            IsUnNamedFunctionPointer = isUnNamedFunctionPointer;
        }

        internal CRecordField(
            CRecordField previous,
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
