// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Tests.Common.Data.Model.C;

[PublicAPI]
public class CTestEnum
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type_integer")]
    public string IntegerType { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public ImmutableArray<CTestEnumValue> Values { get; set; } = ImmutableArray<CTestEnumValue>.Empty;
}
