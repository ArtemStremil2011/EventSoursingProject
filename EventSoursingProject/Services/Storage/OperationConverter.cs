using System.Text.Json;
using System.Text.Json.Serialization;
using EventSoursingProject.Models;

namespace EventSoursingProject.Services.Storage
{
    public class OperationJsonConverter : JsonConverter<Operation>
    {
        public override Operation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            return new Operation
            {
                OperationId = root.GetProperty("OperationId").GetGuid(),
                OperationName = root.GetProperty("OperationName").GetString() ?? string.Empty,
                OperationType = Enum.Parse<OperationType>(root.GetProperty("OperationType").GetString()!),
                Amount = root.GetProperty("Amount").GetDecimal(),
                ScoreId = root.GetProperty("ScoreId").GetGuid(),
                Sequence = root.GetProperty("Sequence").GetInt32(),
                CreatedAt = root.GetProperty("CreatedAt").GetDateTime()
            };
        }

        public override void Write(Utf8JsonWriter writer, Operation value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("OperationId", value.OperationId);
            writer.WriteString("OperationName", value.OperationName);
            writer.WriteString("OperationType", value.OperationType.ToString());
            writer.WriteNumber("Amount", value.Amount);
            writer.WriteString("ScoreId", value.ScoreId);
            writer.WriteNumber("Sequence", value.Sequence);
            writer.WriteString("CreatedAt", value.CreatedAt);

            writer.WriteEndObject();
        }
    }
}