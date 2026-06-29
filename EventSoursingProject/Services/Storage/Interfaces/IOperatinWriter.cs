using EventSoursingProject.Models;

namespace EventSoursingProject.Services.Storage.Interfaces
{
    public interface IOperationWriter
    {
        Task AppendOperationAsync(Operation operation);
        Task AppendOperationsAsync(IEnumerable<Operation> operations);
    }
}