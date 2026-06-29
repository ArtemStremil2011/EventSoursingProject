// ============================================================
//  Services/Storage/JsonOperationWriter.cs
// ============================================================
using EventSoursingProject.Models;
using EventSoursingProject.Services.Storage.Interfaces;
using System.Text.Json;

namespace EventSoursingProject.Services.Storage
{
    public class JsonOperationWriter : IOperationWriter
    {
        private readonly string _filePath;
        private readonly object _fileLock = new();
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonOperationWriter()
        {
            var dataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(dataDir);

            _filePath = Path.Combine(dataDir, "operations.log");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                Converters = { new OperationJsonConverter() }
            };
        }

        public Task AppendOperationAsync(Operation operation)
        {
            lock (_fileLock)
            {
                var json = JsonSerializer.Serialize(operation, _jsonOptions);
                File.AppendAllText(_filePath, json + Environment.NewLine);
            }
            return Task.CompletedTask;
        }

        public Task AppendOperationsAsync(IEnumerable<Operation> operations)
        {
            lock (_fileLock)
            {
                var lines = operations.Select(op => JsonSerializer.Serialize(op, _jsonOptions));
                File.AppendAllLines(_filePath, lines);
            }
            return Task.CompletedTask;
        }
    }
}