// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using C2CS.Feature.ReadCodeC.Data.Model;

namespace C2CS.Feature.ReadCodeC.Data.Serialization;

#pragma warning disable CA1308

public class CLocationJsonConverter : JsonConverter<CLocation>
{
    public override CLocation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<CLocation>(ref reader, options);
        }

        return CLocation.System;
    }

    public override void Write(Utf8JsonWriter writer, CLocation value, JsonSerializerOptions options)
    {
        if (value.IsSystem)
        {
            writer.WriteNullValue();
        }
        else
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
