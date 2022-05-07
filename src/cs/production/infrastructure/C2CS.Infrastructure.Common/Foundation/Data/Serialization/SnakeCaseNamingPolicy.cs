// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Linq;
using System.Text.Json;

namespace C2CS.Foundation.Data.Serialization;

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public static SnakeCaseNamingPolicy Instance { get; } = new();

    public override string ConvertName(string name)
    {
        // Conversion to other naming convention goes here. Like SnakeCase, KebabCase etc.
        return ToSnakeCase(name);
    }

    private static string ToSnakeCase(string str)
    {
#pragma warning disable CA1308
        return string.Concat(
            str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLowerInvariant();
#pragma warning restore CA1308
    }
}
