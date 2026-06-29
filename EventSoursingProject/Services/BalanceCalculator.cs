using EventSoursingProject.Models;

namespace EventSoursingProject.Services
{
    public class BalanceCalculator
    {
        public decimal CalculateBalance(IEnumerable<Operation> operations)
        {
            decimal balance = 0;

            foreach (var op in operations.OrderBy(o => o.Sequence))
            {
                if (op.OperationType == OperationType.Credit)
                    balance += op.Amount;
                else if (op.OperationType == OperationType.Debit)
                    balance -= op.Amount;
            }

            return balance;
        }

        public decimal CalculateBalanceAtDate(IEnumerable<Operation> operations, DateTime date)
        {
            decimal balance = 0;

            foreach (var op in operations
                .Where(o => o.CreatedAt <= date)
                .OrderBy(o => o.Sequence))
            {
                if (op.OperationType == OperationType.Credit)
                    balance += op.Amount;
                else if (op.OperationType == OperationType.Debit)
                    balance -= op.Amount;
            }

            return balance;
        }

        public BalanceStats GetStatistics(IEnumerable<Operation> operations)
        {
            var ops = operations.ToList();

            return new BalanceStats
            {
                TotalCredit = ops.Where(o => o.OperationType == OperationType.Credit).Sum(o => o.Amount),
                TotalDebit = ops.Where(o => o.OperationType == OperationType.Debit).Sum(o => o.Amount),
                OperationCount = ops.Count,
                CreditCount = ops.Count(o => o.OperationType == OperationType.Credit),
                DebitCount = ops.Count(o => o.OperationType == OperationType.Debit),
                FirstOperation = ops.Any() ? ops.Min(o => o.CreatedAt) : null,
                LastOperation = ops.Any() ? ops.Max(o => o.CreatedAt) : null
            };
        }
    }

    public class BalanceStats
    {
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
        public int OperationCount { get; set; }
        public int CreditCount { get; set; }
        public int DebitCount { get; set; }
        public DateTime? FirstOperation { get; set; }
        public DateTime? LastOperation { get; set; }

        public decimal Balance => TotalCredit - TotalDebit;
    }
}