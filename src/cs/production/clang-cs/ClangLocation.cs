// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Text.Json.Serialization;

// NOTE: Properties are required for System.Text.Json serialization
public struct ClangLocation : IComparable<ClangLocation>
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

    public override string ToString()
    {
        if (LineNumber == 0 && LineColumn == 0)
        {
            return $"{FileName}";
        }

        return string.IsNullOrEmpty(FilePath) || FilePath == FileName ?
            $"{FileName}:{LineNumber}:{LineColumn}" :
            $"{FileName}:{LineNumber}:{LineColumn} ({FilePath})";
    }

    public bool Equals(ClangLocation other)
    {
        return FilePath == other.FilePath && LineNumber == other.LineNumber;
    }

    public override bool Equals(object? obj)
    {
        return obj is ClangLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FilePath, LineNumber, LineColumn);
    }

    public int CompareTo(ClangLocation other)
    {
        // ReSharper disable once JoinDeclarationAndInitializer
        int result;

        result = string.Compare(FileName, other.FileName, StringComparison.Ordinal);
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (result != 0)
        {
            return result;
        }

        result = LineNumber.CompareTo(other.LineNumber);

        return result;
    }

    public static bool operator ==(ClangLocation first, ClangLocation second)
    {
        return first.Equals(second);
    }

    public static bool operator !=(ClangLocation first, ClangLocation second)
    {
        return !(first == second);
    }

    public static bool operator <(ClangLocation first, ClangLocation second)
    {
        throw new NotImplementedException();
    }

    public static bool operator >(ClangLocation first, ClangLocation second)
    {
        throw new NotImplementedException();
    }

    public static bool operator >=(ClangLocation first, ClangLocation second)
    {
        throw new NotImplementedException();
    }

    public static bool operator <=(ClangLocation first, ClangLocation second)
    {
        throw new NotImplementedException();
    }
}
