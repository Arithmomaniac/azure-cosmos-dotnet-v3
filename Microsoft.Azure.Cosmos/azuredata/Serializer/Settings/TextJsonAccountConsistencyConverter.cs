﻿//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Azure.Cosmos
{
    using System;
    using System.Globalization;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Microsoft.Azure.Documents;

    internal class TextJsonAccountConsistencyConverter : JsonConverter<AccountConsistency>
    {
        public override AccountConsistency Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException(string.Format(CultureInfo.CurrentCulture, RMResources.JsonUnexpectedToken));
            }

            using JsonDocument json = JsonDocument.ParseValue(ref reader);
            JsonElement root = json.RootElement;
            return TextJsonAccountConsistencyConverter.ReadProperty(root);
        }

        public override void Write(
            Utf8JsonWriter writer,
            AccountConsistency setting,
            JsonSerializerOptions options)
        {
            TextJsonAccountConsistencyConverter.WritePropertyValue(writer, setting, options);
        }

        public static void WritePropertyValue(
            Utf8JsonWriter writer,
            AccountConsistency setting,
            JsonSerializerOptions options)
        {
            if (setting == null)
            {
                return;
            }

            writer.WriteStartObject();

            writer.WriteString(JsonEncodedStrings.DefaultConsistencyLevel, setting.DefaultConsistencyLevel.ToString());

            writer.WriteNumber(JsonEncodedStrings.MaxStalenessPrefix, setting.MaxStalenessPrefix);

            writer.WriteNumber(JsonEncodedStrings.MaxStalenessIntervalInSeconds, setting.MaxStalenessIntervalInSeconds);

            writer.WriteEndObject();
        }

        public static AccountConsistency ReadProperty(JsonElement root)
        {
            AccountConsistency setting = new AccountConsistency();
            foreach (JsonProperty property in root.EnumerateObject())
            {
                TextJsonAccountConsistencyConverter.ReadPropertyValue(setting, property);
            }

            return setting;
        }

        private static void ReadPropertyValue(
            AccountConsistency setting,
            JsonProperty property)
        {
            if (property.NameEquals(JsonEncodedStrings.MaxStalenessPrefix.EncodedUtf8Bytes))
            {
                setting.MaxStalenessPrefix = property.Value.GetInt32();
            }
            else if (property.NameEquals(JsonEncodedStrings.MaxStalenessIntervalInSeconds.EncodedUtf8Bytes))
            {
                setting.MaxStalenessIntervalInSeconds = property.Value.GetInt32();
            }
            else if (property.NameEquals(JsonEncodedStrings.DefaultConsistencyLevel.EncodedUtf8Bytes))
            {
                TextJsonSettingsHelper.TryParseEnum<ConsistencyLevel>(property, consistencyLevel => setting.DefaultConsistencyLevel = consistencyLevel);
            }
        }
    }
}