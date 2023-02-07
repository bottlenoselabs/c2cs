// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace C2CS.Tests.C.Data.Models;

[PublicAPI]
public class CTestFunction
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("calling_convention")]
    public string CallingConvention { get; set; } = string.Empty;

    [JsonPropertyName("return_type_name")]
    public string ReturnTypeName { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public ImmutableArray<CTestFunctionParameter> Parameters { get; set; }

    public override string ToString()
    {
        return Name;
    }
}
