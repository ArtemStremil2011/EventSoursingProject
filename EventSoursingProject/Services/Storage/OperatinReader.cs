using EventSoursingProject.Models;
using EventSoursingProject.Services.Storage.Interfaces;
using System.Text.Json;

namespace EventSoursingProject.Services.Storage
{
    public class JsonOperationReader : IOperationReader
    {
        private readonly string _filePath;
        private readonly object _fileLock = new();
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonOperationReader()
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

        public Task<List<Operation>> GetAllOperationsAsync()
        {
            return Task.FromResult(ReadAllFromFile());
        }

        public Task<List<Operation>> GetOperationsByScoreAsync(Guid scoreId)
        {
            var all = ReadAllFromFile();
            return Task.FromResult(all.Where(o => o.ScoreId == scoreId).ToList());
        }

        private List<Operation> ReadAllFromFile()
        {
            if (!File.Exists(_filePath))
                return new List<Operation>();

            lock (_fileLock)
            {
                try
                {
                    var lines = File.ReadAllLines(_filePath);
                    return lines
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .Select(l => JsonSerializer.Deserialize<Operation>(l, _jsonOptions)!)
                        .ToList();
                }
                catch
                {
                    return new List<Operation>();
                }
            }
        }
    }
}