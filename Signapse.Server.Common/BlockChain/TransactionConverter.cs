using Signapse.BlockChain.Transactions;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Signapse.BlockChain
{
    public class TransactionConverter : JsonConverter<ITransaction>
    {
        private class TransactionShell : ITransaction
        {
            public TransactionType TransactionType { get; set; }
        }

        public override bool CanConvert(Type typeToConvert)
            => typeof(ITransaction) == typeToConvert;

        public override ITransaction? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var shellReader = reader;
            if (JsonSerializer.Deserialize<TransactionShell>(ref shellReader) is TransactionShell shell)
            {
                return shell.TransactionType switch
                {
                    TransactionType.Genesis => JsonSerializer.Deserialize<GenesisTransaction>(ref reader, options),
                    TransactionType.Member => JsonSerializer.Deserialize<MemberTransaction>(ref reader, options),
                    TransactionType.Content => JsonSerializer.Deserialize<ContentTransaction>(ref reader, options),
                    TransactionType.JoinAffiliate => JsonSerializer.Deserialize<JoinTransaction>(ref reader, options),
                    _ => JsonSerializer.Deserialize<TransactionShell>(ref reader, options)
                };
            }

            return null;
        }

        public override void Write(
            Utf8JsonWriter writer,
            ITransaction value,
            JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType());
        }
    }
}