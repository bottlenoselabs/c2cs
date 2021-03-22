// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory (https://github.com/lithiumtoast/c2cs) for full license information.

using System.Collections.Immutable;

namespace C2CS.CSharp
{
    public record CSharpStruct : CSharpCommon
    {
        public readonly CSharpType Type;
        public readonly ImmutableArray<CSharpStructField> Fields;
        public readonly ImmutableArray<CSharpStruct> NestedStructs;

        public CSharpStruct(
            string name,
            string codeLocationComment,
            CSharpType type,
            ImmutableArray<CSharpStructField> fields)
            : this(name, codeLocationComment, type, fields, ImmutableArray<CSharpStruct>.Empty)
        {
        }

        public CSharpStruct(
            string name,
            string codeLocationComment,
            CSharpType type,
            ImmutableArray<CSharpStructField> fields,
            ImmutableArray<CSharpStruct> nestedStructs)
            : base(name, codeLocationComment)
        {
            Type = type;
            Fields = fields;
            NestedStructs = nestedStructs;
        }

        // Required for debugger string with records
        // ReSharper disable once RedundantOverriddenMember
        public override string ToString()
        {
            return base.ToString();
        }
    }
}
