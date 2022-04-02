// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C2CS.Serialization;

public class TargetPlatformJsonConverter : JsonConverter<NativePlatform>
{
    public override NativePlatform Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return NativePlatform.Unknown;
        }

        var result = new NativePlatform(value);
        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        NativePlatform value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Target);
    }
}
