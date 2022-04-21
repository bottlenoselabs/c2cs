// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Tests.Common.Data.Model;

[PublicAPI]
public class CSharpGeneratedStructLayout
{
    [JsonPropertyName("kind")]
    public string LayoutKind { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("pack")]
    public int Pack { get; set; }
}
