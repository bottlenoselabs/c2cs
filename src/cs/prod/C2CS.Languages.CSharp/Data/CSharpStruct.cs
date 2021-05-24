// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;

namespace C2CS.CSharp
{
    public record CSharpStruct : CSharpNode
    {
        public readonly CSharpType Type;
        public readonly ImmutableArray<CSharpStructField> Fields;
        public readonly ImmutableArray<CSharpNode> NestedNodes;

        public CSharpStruct(
            string codeLocationComment,
            CSharpType type,
            ImmutableArray<CSharpStructField> fields)
            : this(codeLocationComment, type, fields, ImmutableArray<CSharpNode>.Empty)
        {
        }

        public CSharpStruct(
            string codeLocationComment,
            CSharpType type,
            ImmutableArray<CSharpStructField> fields,
            ImmutableArray<CSharpNode> nestedNodes)
            : base(type.Name, codeLocationComment)
        {
            Type = type;
            Fields = fields;
            NestedNodes = nestedNodes;
        }

        // Required for debugger string with records
        // ReSharper disable once RedundantOverriddenMember
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
