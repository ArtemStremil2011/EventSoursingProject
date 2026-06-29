using EventSoursingProject.Models;

namespace EventSoursingProject.Services.Storage.Interfaces
{
    public interface IOperationReader
    {
        public  Task<List<Operation>> GetAllOperationsAsync();
        public  Task<List<Operation>> GetOperationsByScoreAsync(Guid ScoreId);
    }
}
