// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.UseCases.AbstractSyntaxTreeC
{
    // NOTE: Properties are required for System.Text.Json serialization
    [PublicAPI]
    public record CNode : IComparable<CNode>
    {
        [JsonPropertyName("location")]
        public ClangLocation Location { get; set; }

        [JsonIgnore]
        public CKind Kind => GetKind();

        private CKind GetKind()
        {
            return this switch
            {
                CEnum => CKind.Enum,
                CEnumValue => CKind.EnumValue,
                CFunction => CKind.Function,
                CFunctionParameter => CKind.FunctionParameter,
                CFunctionPointer => CKind.FunctionPointer,
                CFunctionPointerParameter => CKind.FunctionPointerParameter,
                COpaqueType => CKind.OpaqueType,
                CRecord => CKind.Record,
                CRecordField => CKind.RecordField,
                CTypedef => CKind.Typedef,
                CVariable => CKind.Variable,
                _ => CKind.Unknown
            };
        }

        public int CompareTo(CNode? other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            var result = Location.CompareTo(other.Location);
            return result;
        }

        public static bool operator <(CNode first, CNode second)
        {
            throw new NotImplementedException();
        }

        public static bool operator >(CNode first, CNode second)
        {
            throw new NotImplementedException();
        }

        public static bool operator >=(CNode first, CNode second)
        {
            throw new NotImplementedException();
        }

        public static bool operator <=(CNode first, CNode second)
        {
            throw new NotImplementedException();
        }
    }
}
