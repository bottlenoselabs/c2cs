// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Feature.ExtractAbstractSyntaxTreeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization
[PublicAPI]
public record CNode : IComparable<CNode>
{
    [JsonPropertyName("location")]
    public CLocation Location { get; set; }

    [JsonIgnore]
    public CKind Kind => GetKind();

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

    private CKind GetKind()
    {
        return this switch
        {
            CEnum => CKind.Enum,
            CEnumValue => CKind.EnumValue,
            CFunction => CKind.Function,
            CFunctionPointer => CKind.FunctionPointer,
            COpaqueType => CKind.OpaqueType,
            CRecord => CKind.Record,
            CTypedef => CKind.Typedef,
            CVariable => CKind.Variable,
            CMacroDefinition => CKind.MacroDefinition,
            _ => CKind.Unknown
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
