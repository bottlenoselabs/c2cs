// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Text.Json.Serialization;

// NOTE: Properties are required for System.Text.Json serialization
public record struct ClangLocation : IComparable<ClangLocation>
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; }

    [JsonPropertyName("filePath")]
    public string FilePath { get; set; }

    [JsonPropertyName("line")]
    public int LineNumber { get; set; }

    [JsonPropertyName("column")]
    public int LineColumn { get; set; }

    [JsonPropertyName("isBuiltin")]
    public bool IsBuiltin { get; set; }

#pragma warning disable CA2211
    public static ClangLocation BuiltIn = new()
#pragma warning restore CA2211
    {
        FilePath = string.Empty,
        FileName = "Builtin",
        LineColumn = 0,
        LineNumber = 0,
        IsBuiltin = true
    };

    public override string ToString()
    {
        if (LineNumber == 0 && LineColumn == 0)
        {
            return $"{FileName}";
        }

        return string.IsNullOrEmpty(FilePath) || FilePath == FileName
            ? $"{FileName}:{LineNumber}:{LineColumn}"
            : $"{FileName}:{LineNumber}:{LineColumn} ({FilePath})";
    }

    public int CompareTo(ClangLocation other)
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

    public static bool operator <(ClangLocation first, ClangLocation second)
    {
        return first.CompareTo(second) < 0;
    }

    public static bool operator >(ClangLocation first, ClangLocation second)
    {
        return first.CompareTo(second) > 0;
    }

    public static bool operator >=(ClangLocation first, ClangLocation second)
    {
        return first.CompareTo(second) >= 0;
    }

    public static bool operator <=(ClangLocation first, ClangLocation second)
    {
        return first.CompareTo(second) <= 0;
    }
}
