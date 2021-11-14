// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.UseCases.CExtractAbstractSyntaxTree;

// NOTE: Properties are required for System.Text.Json serialization
[PublicAPI]
public class CType
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public CKind Kind { get; set; } = CKind.Unknown;

    [JsonPropertyName("sizeOf")]
    public int SizeOf { get; set; }

    [JsonPropertyName("alignOf")]
    public int? AlignOf { get; set; }

    [JsonPropertyName("sizeOfElement")]
    public int? ElementSize { get; set; }

    [JsonPropertyName("arraySize")]
    public int? ArraySize { get; set; }

    [JsonPropertyName("isSystem")]
    public bool IsSystem { get; set; }

    [JsonPropertyName("isAnonymous")]
    public bool IsAnonymous { get; set; }

    [JsonPropertyName("location")]
    public ClangLocation? Location { get; set; }

    public override string ToString()
    {
        return Name;
    }
}
