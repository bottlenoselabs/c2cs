// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record ClangRecord : ClangNode
    {
        public readonly ClangType Type;
        public readonly ImmutableArray<ClangRecordField> Fields;
        public readonly ImmutableArray<ClangNode> NestedNodes;

        internal ClangRecord(
            ClangCodeLocation codeLocation,
            ClangType type,
            ImmutableArray<ClangRecordField> fields,
            ImmutableArray<ClangNode> nestedNodes)
            : base(ClangNodeKind.Record, codeLocation)
        {
            Type = type;
            Fields = fields;
            NestedNodes = nestedNodes;
        }

        public override string ToString()
        {
            return $"Record {Type.Name} @ {CodeLocation.ToString()}";
        }
    }
}
