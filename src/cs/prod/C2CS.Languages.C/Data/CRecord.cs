// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.Languages.C
{
    public record CRecord : CNode
    {
        public readonly CType Type;
        public readonly ImmutableArray<CRecordField> Fields;
        public readonly ImmutableArray<CNode> NestedNodes;

        internal CRecord(
            CCodeLocation codeLocation,
            CType type,
            ImmutableArray<CRecordField> fields,
            ImmutableArray<CNode> nestedNodes)
            : base(CNodeKind.Record, codeLocation)
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
