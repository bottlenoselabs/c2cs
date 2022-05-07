// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;

namespace C2CS.Contexts.ReadCodeC.Data.Model;

// NOTE: Properties are required for System.Text.Json serialization

public record struct CLocation : IComparable<CLocation>
{
#pragma warning disable CA2211
    public static CLocation NoLocation = new()
    {
        IsNull = true
    };
#pragma warning restore CA2211

    [JsonPropertyName("fileName")]
    public string FileName { get; set; }

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; }

    [JsonPropertyName("line")]
    public int LineNumber { get; set; }

    [JsonPropertyName("column")]
    public int LineColumn { get; set; }

    [JsonIgnore]
    public bool IsNull { get; set; }

    public override string ToString()
    {
#pragma warning disable CA1308
        if (IsNull)
        {
            return nameof(NoLocation);
        }
#pragma warning restore CA1308

        if (LineNumber == 0 && LineColumn == 0)
        {
            return $"{FileName}";
        }

        return string.IsNullOrEmpty(FilePath) || FilePath == FileName
            ? $"{FileName}:{LineNumber}:{LineColumn}"
            : $"{FileName}:{LineNumber}:{LineColumn} ({FilePath})";
    }

    public int CompareTo(CLocation other)
    {
        var result = string.Compare(FileName, other.FileName, StringComparison.Ordinal);
        if (result != 0)
        {
            return result;
        }

        result = LineNumber.CompareTo(other.LineNumber);
        if (result != 0)
        {
            return result;
        }

        result = LineColumn.CompareTo(other.LineColumn);
        if (result != 0)
        {
            return result;
        }

        return result;
    }

    public static bool operator <(CLocation first, CLocation second)
    {
        return first.CompareTo(second) < 0;
    }

    public static bool operator >(CLocation first, CLocation second)
    {
        return first.CompareTo(second) > 0;
    }

    public static bool operator >=(CLocation first, CLocation second)
    {
        return first.CompareTo(second) >= 0;
    }

    public static bool operator <=(CLocation first, CLocation second)
    {
        return first.CompareTo(second) <= 0;
    }
}
