// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;

namespace C2CS.Contexts.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
public abstract record CNode : IComparable<CNode>
{
    [JsonIgnore]
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public CKind Kind => GetKind(this);

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

        var result = CompareToInternal(other);
        return result;
    }

    protected virtual int CompareToInternal(CNode? other)
    {
        return 0;
    }

    private CKind GetKind(CNode node)
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
            CRecord => ((CRecord)node).RecordKind == CRecordKind.Struct ? CKind.Struct : CKind.Union,
            CTypeAlias => CKind.TypeAlias,
            CVariable => CKind.Variable,
            CMacroObject => CKind.Macro,
            CRecordField => CKind.RecordField,
            CPrimitive => CKind.Primitive,
            CPointer => CKind.Pointer,
            CArray => CKind.Array,
            _ => throw new NotImplementedException($"The kind of mapping for '{GetType()}' is not implemented.")
        };
    }

    public static bool operator <(CNode first, CNode second)
    {
        return first.CompareTo(second) < 0;
    }

    public static bool operator >(CNode first, CNode second)
    {
        return first.CompareTo(second) > 0;
    }

    public static bool operator >=(CNode first, CNode second)
    {
        return first.CompareTo(second) >= 0;
    }

    public static bool operator <=(CNode first, CNode second)
    {
        return first.CompareTo(second) <= 0;
    }
}
