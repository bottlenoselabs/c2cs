// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json.Serialization;

namespace C2CS.Tests.Common.Data.Model;

public class CSharpGeneratedFunctionParameter
{
    [JsonPropertyName("type_name")]
    public string TypeName { get; set; } = string.Empty;
}
