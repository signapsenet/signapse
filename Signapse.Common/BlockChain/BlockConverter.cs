using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signapse.BlockChain
{
    public class BlockConverter : JsonConverter<IBlock>
    {
        public override bool CanConvert(Type typeToConvert)
            => typeToConvert == typeof(IBlock);

        public override IBlock? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<Block>(ref reader, options);
        }

        public override void Write(
            Utf8JsonWriter writer,
            IBlock value,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}