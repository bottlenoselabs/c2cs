// Copyright (c) Lucas Girouard-Stranks (https://github.com/lithiumtoast). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Text.Json.Serialization;

// NOTE: Properties are required for System.Text.Json serialization
public struct ClangLocation : IComparable<ClangLocation>
{
    [JsonPropertyName("file")]
    public string Path { get; set; }

    [JsonPropertyName("line")]
    public int LineNumber { get; set; }

    [JsonPropertyName("column")]
    public int LineColumn { get; set; }

    [JsonPropertyName("isSystem")]
    public bool IsSystem { get; set; }

    public override string ToString()
    {
        if (LineNumber == 0 && LineColumn == 0)
        {
            return $"{Path}";
        }

        return $"{Path}:{LineNumber}:{LineColumn}";
    }

    public bool Equals(ClangLocation other)
    {
        return Path == other.Path && LineNumber == other.LineNumber;
    }

    public override bool Equals(object? obj)
    {
        return obj is ClangLocation other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Path, LineNumber);
    }

    public int CompareTo(ClangLocation other)
    {
        // ReSharper disable once JoinDeclarationAndInitializer
        int result;

        result = string.Compare(Path, other.Path, StringComparison.Ordinal);
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
