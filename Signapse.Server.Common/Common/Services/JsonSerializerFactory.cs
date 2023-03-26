using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Signapse.Services
{
    public class JsonSerializerFactory
    {
        public JsonSerializerOptions Options { get; }

        public JsonSerializerFactory(IServiceProvider provider)
        {
            this.Options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = {
                    new BlockChain.BlockConverter(),
                    new BlockChain.TransactionConverter(),
                    new DIConverter(provider)
                }
            };
        }

        public string Serialize<T>(T obj)
        {
            if (obj != null)
            {
                return JsonSerializer.Serialize(obj, obj.GetType(), this.Options);
            }
            else
            {
                return "";
            }
        }

        public object? Deserialize(Type type, string json)
        {
            try
            {
                return JsonSerializer.Deserialize(json, type, this.Options);
            }
            catch { return null; }
        }

        public T? Deserialize<T>(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    return default(T);
                }
                else
                {
                    return JsonSerializer.Deserialize<T>(json, this.Options);
                }
            }
            catch { return default(T); }
        }

        public T? Deserialize<T>(Stream stream)
        {
            try
            {
                return (T?)JsonSerializer.Deserialize(stream, typeof(T), this.Options);
            }
            catch { return default(T); }
        }

        class DIConverter : JsonConverter<object>
        {
            readonly IServiceProvider provider;

            public DIConverter(IServiceProvider provider)
                => this.provider = provider;

            public override bool CanConvert(Type typeToConvert)
                => typeToConvert.Namespace?.StartsWith("System") == false
                    && typeToConvert.IsClass
                    && typeToConvert.GetConstructors()
                        .Where(ctor => ctor.GetParameters().All(p=>p.HasDefaultValue))
                        .Any() == false;

            public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var res = ActivatorUtilities.CreateInstance(provider, typeToConvert);

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                var pDict = typeToConvert
                    .GetProperties()
                    .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return res;
                    }

                    // Get the key.
                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException();
                    }

                    if (reader.GetString() is string propertyName)
                    {
                        reader.Read();
                        if (pDict.TryGetValue(propertyName, out var pi))
                        {
                            // Get the value.
                            object? value = reader.TokenType switch
                            {
                                JsonTokenType.True => true,
                                JsonTokenType.False => false,
                                JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
                                JsonTokenType.Number => reader.GetDouble(),
                                JsonTokenType.String when reader.TryGetDateTime(out DateTime datetime) => datetime,
                                JsonTokenType.String => reader.GetString()!,
                                _ => JsonSerializer.Deserialize(ref reader, pi.PropertyType, options)
                            };

                            if (value is string str)
                            {
                                value = TypeDescriptor.GetConverter(pi.PropertyType)
                                    .ConvertFromInvariantString(str)!;
                                pi.SetValue(res, value);
                            }
                            else
                            {
                                pi.SetValue(res, Convert.ChangeType(value, pi.PropertyType));
                            }
                        }
                    }
                    else
                    {
                        throw new JsonException();
                    }
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                foreach (var pi in value.GetType().GetProperties())
                {
                    writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(pi.Name) ?? pi.Name);
                    JsonSerializer.Serialize(writer, pi.GetValue(value), pi.PropertyType, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}