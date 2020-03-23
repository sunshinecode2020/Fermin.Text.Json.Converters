﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;

namespace Fermin.Text.Json.Converters
{
    public class DynamicConverter : JsonConverter<dynamic>
    {
        public override dynamic Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            // Map JsonToken types to primatives
            // or other commonly used types
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number => ReadNumber(ref reader),
                JsonTokenType.String => ReadString(ref reader),
                JsonTokenType.StartObject => ReadObject(ref reader),
                _ => DefaultCase(ref reader)
            };
        }

        public override void Write(
            Utf8JsonWriter writer,
            object value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        private JsonElement DefaultCase(ref Utf8JsonReader reader)
        {
            // Use JsonElement as fallback.
            JsonDocument document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.Clone();
        }

        private dynamic ReadString(ref Utf8JsonReader reader)
        {
            if (reader.TryGetDateTime(out DateTime datetime))
            {
                return datetime;
            }

            return reader.GetString();
        }

        private double ReadNumber(ref Utf8JsonReader reader)
        {
            if (reader.TryGetInt64(out long l))
            {
                return (double)l;
            }

            return reader.GetDouble();
        }

        private object ReadObject(ref Utf8JsonReader reader)
        {
            using JsonDocument documentV = JsonDocument.ParseValue(ref reader);
            return ReadObject(documentV.RootElement);
        }

        private object ReadObject(JsonElement jsonElement)
        {
            IDictionary<string, object> expandoObject = new ExpandoObject();
            foreach (var obj in jsonElement.EnumerateObject())
            {
                var k = obj.Name;
                var value = ReadValue(obj.Value);
                expandoObject[k] = value;
            }
            return expandoObject;
        }

        private object? ReadValue(JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => jsonElement.ToString(), // may need to deal with datetime and other things stored as strings in json
                JsonValueKind.Number => jsonElement.TryGetInt64(out long l)? l : 0, // may need to deal with other number types
                JsonValueKind.Object => ReadObject(jsonElement),
                JsonValueKind.Array => ReadList(jsonElement),
                JsonValueKind.Undefined => null, 
                JsonValueKind.Null => null,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private object? ReadList(JsonElement jsonElement)
        {
            IList<object?> list = new List<object?>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                list.Add(ReadValue(item));
            }
            return list.Count == 0 ? null : list;
        }
    }
}