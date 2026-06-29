namespace EventSoursingProject.Models
{
    public class Operation
    {
        public Guid OperationId { get; set; }          
        public string OperationName { get; set; } = string.Empty; 
        public OperationType OperationType { get; set; }
        public decimal Amount { get; set; }            
        public Guid ScoreId { get; set; }
        public Score? Score { get; set; }
        public int Sequence { get; set; }              
        public DateTime CreatedAt { get; set; }        
    }
}
