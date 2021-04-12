// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

namespace C2CS.Languages.C
{
    public record ClangRecordField : ClangCommon
    {
        public readonly ClangType Type;
        public readonly int Offset;
        public readonly int Padding;

        internal ClangRecordField(
            string name,
            ClangCodeLocation codeLocation,
            ClangType type,
            int offset)
            : base(ClangKind.RecordField, name, codeLocation)
        {
            Type = type;
            Offset = offset;
            Padding = 0;
        }

        internal ClangRecordField(
            ClangRecordField previous,
            int padding)
            : this(previous.Name, previous.CodeLocation, previous.Type, previous.Offset)
        {
            Padding = padding;
        }

        // Required for debugger string with records
        // ReSharper disable once RedundantOverriddenMember
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
