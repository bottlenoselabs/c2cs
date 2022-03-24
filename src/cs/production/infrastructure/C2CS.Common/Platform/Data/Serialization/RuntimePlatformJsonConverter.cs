// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace C2CS.Serialization;

public class RuntimePlatformJsonConverter : JsonConverter<RuntimePlatform>
{
    public override RuntimePlatform Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return new RuntimePlatform(RuntimeOperatingSystem.Unknown, RuntimeArchitecture.Unknown);
        }

        var result = RuntimePlatform.FromString(value);
        if (result == RuntimePlatform.Unknown)
        {
            throw new JsonException($"Unknown runtime platform '{value}'.");
        }

        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        RuntimePlatform value,
        JsonSerializerOptions options)
    {
        var stringValue = string.Empty;

        if (value == RuntimePlatform.win_x64)
        {
            stringValue = "win-x64";
        }

        if (value == RuntimePlatform.osx_x64)
        {
            stringValue = "osx-x64";
        }

        if (value == RuntimePlatform.osx_arm64)
        {
            stringValue = "osx-arm64";
        }

        if (value == RuntimePlatform.linux_x64)
        {
            stringValue = "linux-x64";
        }

        writer.WriteStringValue(stringValue);
    }
}
