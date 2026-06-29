using EventSoursingProject.Models;
using EventSoursingProject.Services.Storage.Interfaces;

namespace EventSoursingProject.Services
{
    public class CommandExecutor
    {
        private readonly IOperationReader _reader;
        private readonly IOperationWriter _writer;
        private readonly BalanceCalculator _calculator;

        // Хранилище счетов в памяти
        private readonly Dictionary<Guid, Score> _scores = new();

        public CommandExecutor(
            IOperationReader reader,
            IOperationWriter writer,
            BalanceCalculator calculator)
        {
            _reader = reader;
            _writer = writer;
            _calculator = calculator;

            LoadScoresFromOperations().Wait();
        }

        private async Task LoadScoresFromOperations()
        {
            var operations = await _reader.GetAllOperationsAsync();
            var scoreIds = operations.Select(o => o.ScoreId).Distinct();

            foreach (var scoreId in scoreIds)
            {
                if (!_scores.ContainsKey(scoreId))
                {
                    var ops = operations.Where(o => o.ScoreId == scoreId).ToList();
                    var balance = _calculator.CalculateBalance(ops);
                    var lastSeq = ops.Any() ? ops.Max(o => o.Sequence) : 0;

                    _scores[scoreId] = new Score
                    {
                        ScoreId = scoreId,
                        ScoreName = $"Счет {scoreId.ToString().Substring(0, 8)}",
                        OwnerId = Guid.Empty, 
                        LastEventSequence = lastSeq,
                        UpdatedAt = ops.Any() ? ops.Max(o => o.CreatedAt) : DateTime.UtcNow
                    };
                }
            }
        }

        public async Task<string> ExecuteAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "⚠️  Введите команду. Для справки введите 'help'";

            var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLower();

            return command switch
            {
                "help" => ShowHelp(),
                "create" => await CreateScoreAsync(parts),
                "deposit" or "+" => await DepositAsync(parts),
                "withdraw" or "-" => await WithdrawAsync(parts),
                "balance" => await GetBalanceAsync(parts),
                "history" => await GetHistoryAsync(parts),
                "stats" => await GetStatsAsync(parts),
                "scores" => await ListScoresAsync(),
                "clear" => ClearConsole(),
                "exit" => "exit",
                _ => $"❌ Неизвестная команда: '{command}'. Введите 'help' для справки."
            };
        }

        private string ShowHelp()
        {
            return @"
╔═══════════════════════════════════════════════════════════════╗
║                     ДОСТУПНЫЕ КОМАНДЫ                        ║
╠═══════════════════════════════════════════════════════════════╣
║  create <name>        - Создать новый счет                   ║
║  deposit <id> <sum>   - Пополнить счет (+sum)               ║
║  + <id> <sum>         - Пополнить счет (краткая форма)      ║
║  withdraw <id> <sum>  - Снять со счета (-sum)               ║
║  - <id> <sum>         - Снять со счета (краткая форма)      ║
║  balance <id>         - Показать баланс счета               ║
║  history <id>         - Показать историю операций           ║
║  stats <id>           - Показать статистику                 ║
║  scores               - Показать все счета                  ║
║  clear                - Очистить экран                      ║
║  help                 - Показать эту справку                ║
║  exit                 - Выйти из программы                  ║
╚═══════════════════════════════════════════════════════════════╝
";
        }

        private async Task<string> CreateScoreAsync(string[] parts)
        {
            if (parts.Length < 2)
                return "❌ Использование: create <название счета>";

            var scoreName = parts[1];
            var scoreId = Guid.NewGuid();

            var score = new Score
            {
                ScoreId = scoreId,
                ScoreName = scoreName,
                OwnerId = Guid.NewGuid(),
                LastEventSequence = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _scores[scoreId] = score;

            var operation = new Operation
            {
                OperationId = Guid.NewGuid(),
                OperationName = $"Создание счета '{scoreName}'",
                OperationType = OperationType.Credit,
                Amount = 0,
                ScoreId = scoreId,
                Sequence = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _writer.AppendOperationAsync(operation);

            return $"✅ Счет создан!\n" +
                   $"   ID: {scoreId}\n" +
                   $"   Название: {scoreName}\n" +
                   $"   Баланс: 0.00 руб.";
        }

        private async Task<string> DepositAsync(string[] parts)
        {
            if (parts.Length < 3)
                return "❌ Использование: deposit <ID счета> <сумма>";

            if (!Guid.TryParse(parts[1], out var scoreId))
                return $"❌ Неверный формат ID: {parts[1]}";

            if (!decimal.TryParse(parts[2], out var amount) || amount <= 0)
                return $"❌ Сумма должна быть положительным числом: {parts[2]}";

            if (!_scores.ContainsKey(scoreId))
                return $"❌ Счет с ID {scoreId} не найден";

            var operations = await _reader.GetOperationsByScoreAsync(scoreId);
            var lastSeq = operations.Any() ? operations.Max(o => o.Sequence) : 0;

            var operation = new Operation
            {
                OperationId = Guid.NewGuid(),
                OperationName = "Пополнение",
                OperationType = OperationType.Credit,
                Amount = amount,
                ScoreId = scoreId,
                Sequence = lastSeq + 1,
                CreatedAt = DateTime.UtcNow
            };

            await _writer.AppendOperationAsync(operation);

            _scores[scoreId].LastEventSequence = operation.Sequence;
            _scores[scoreId].UpdatedAt = operation.CreatedAt;

            var newBalance = _calculator.CalculateBalance(await _reader.GetOperationsByScoreAsync(scoreId));

            return $"✅ Пополнение на {amount:F2} руб.\n" +
                   $"   Новый баланс: {newBalance:F2} руб.";
        }

        private async Task<string> WithdrawAsync(string[] parts)
        {
            if (parts.Length < 3)
                return "❌ Использование: withdraw <ID счета> <сумма>";

            if (!Guid.TryParse(parts[1], out var scoreId))
                return $"❌ Неверный формат ID: {parts[1]}";

            if (!decimal.TryParse(parts[2], out var amount) || amount <= 0)
                return $"❌ Сумма должна быть положительным числом: {parts[2]}";

            if (!_scores.ContainsKey(scoreId))
                return $"❌ Счет с ID {scoreId} не найден";

            var operations = await _reader.GetOperationsByScoreAsync(scoreId);
            var currentBalance = _calculator.CalculateBalance(operations);

            if (currentBalance < amount)
                return $"❌ Недостаточно средств!\n" +
                       $"   Баланс: {currentBalance:F2} руб.\n" +
                       $"   Запрошено: {amount:F2} руб.";

            var lastSeq = operations.Any() ? operations.Max(o => o.Sequence) : 0;

            var operation = new Operation
            {
                OperationId = Guid.NewGuid(),
                OperationName = "Списание",
                OperationType = OperationType.Debit,
                Amount = amount,
                ScoreId = scoreId,
                Sequence = lastSeq + 1,
                CreatedAt = DateTime.UtcNow
            };

            await _writer.AppendOperationAsync(operation);

            _scores[scoreId].LastEventSequence = operation.Sequence;
            _scores[scoreId].UpdatedAt = operation.CreatedAt;

            var newBalance = _calculator.CalculateBalance(await _reader.GetOperationsByScoreAsync(scoreId));

            return $"✅ Списание {amount:F2} руб.\n" +
                   $"   Новый баланс: {newBalance:F2} руб.";
        }

        private async Task<string> GetBalanceAsync(string[] parts)
        {
            if (parts.Length < 2)
                return "❌ Использование: balance <ID счета>";

            if (!Guid.TryParse(parts[1], out var scoreId))
                return $"❌ Неверный формат ID: {parts[1]}";

            if (!_scores.ContainsKey(scoreId))
                return $"❌ Счет с ID {scoreId} не найден";

            var operations = await _reader.GetOperationsByScoreAsync(scoreId);
            var balance = _calculator.CalculateBalance(operations);

            var score = _scores[scoreId];

            return $"💰 Баланс счета '{score.ScoreName}':\n" +
                   $"   ID: {scoreId}\n" +
                   $"   Баланс: {balance:F2} руб.\n" +
                   $"   Всего операций: {operations.Count}\n" +
                   $"   Последнее обновление: {score.UpdatedAt:dd.MM.yyyy HH:mm:ss}";
        }

        private async Task<string> GetHistoryAsync(string[] parts)
        {
            if (parts.Length < 2)
                return "❌ Использование: history <ID счета> [количество]";

            if (!Guid.TryParse(parts[1], out var scoreId))
                return $"❌ Неверный формат ID: {parts[1]}";

            var limit = parts.Length > 2 && int.TryParse(parts[2], out var l) ? l : 20;

            if (!_scores.ContainsKey(scoreId))
                return $"❌ Счет с ID {scoreId} не найден";

            var operations = await _reader.GetOperationsByScoreAsync(scoreId);
            var sortedOps = operations.OrderByDescending(o => o.Sequence).Take(limit).Reverse();

            if (!sortedOps.Any())
                return $"📭 Нет операций для счета {scoreId}";

            var result = $"📜 ИСТОРИЯ ОПЕРАЦИЙ (последние {sortedOps.Count()}):\n";
            result += "═══════════════════════════════════════════════════════\n";

            foreach (var op in sortedOps)
            {
                var sign = op.OperationType == OperationType.Credit ? "+" : "-";
                result += $"[{op.Sequence}] {op.CreatedAt:dd.MM.yyyy HH:mm:ss}\n";
                result += $"   {op.OperationName}: {sign}{op.Amount:F2} руб.\n";
            }

            var balance = _calculator.CalculateBalance(operations);
            result += $"═══════════════════════════════════════════════════════\n";
            result += $"💰 ИТОГОВЫЙ БАЛАНС: {balance:F2} руб.";

            return result;
        }

        private async Task<string> GetStatsAsync(string[] parts)
        {
            if (parts.Length < 2)
                return "❌ Использование: stats <ID счета>";

            if (!Guid.TryParse(parts[1], out var scoreId))
                return $"❌ Неверный формат ID: {parts[1]}";

            if (!_scores.ContainsKey(scoreId))
                return $"❌ Счет с ID {scoreId} не найден";

            var operations = await _reader.GetOperationsByScoreAsync(scoreId);
            var stats = _calculator.GetStatistics(operations);

            return $@"📊 СТАТИСТИКА СЧЕТА
═══════════════════════════════════════════
Название: {_scores[scoreId].ScoreName}
ID: {scoreId}

💰 БАЛАНС: {stats.Balance:F2} руб.

📈 ОПЕРАЦИИ:
   Всего: {stats.OperationCount}
   Пополнений: {stats.CreditCount} (всего {stats.TotalCredit:F2} руб.)
   Списаний: {stats.DebitCount} (всего {stats.TotalDebit:F2} руб.)

⏰ ПЕРИОД:
   Первая: {stats.FirstOperation?.ToString("dd.MM.yyyy HH:mm:ss") ?? "Нет данных"}
   Последняя: {stats.LastOperation?.ToString("dd.MM.yyyy HH:mm:ss") ?? "Нет данных"}";
        }

        private async Task<string> ListScoresAsync()
        {
            if (!_scores.Any())
                return "📭 Нет созданных счетов. Создайте счет командой 'create <название>'";

            var result = "📋 СПИСОК СЧЕТОВ\n";
            result += "═══════════════════════════════════════════\n";

            foreach (var kvp in _scores)
            {
                var scoreId = kvp.Key;
                var score = kvp.Value;
                var operations = await _reader.GetOperationsByScoreAsync(scoreId);
                var balance = _calculator.CalculateBalance(operations);

                result += $"🔹 {score.ScoreName}\n";
                result += $"   ID: {scoreId}\n";
                result += $"   Баланс: {balance:F2} руб.\n";
                result += $"   Операций: {operations.Count}\n\n";
            }

            return result;
        }

        private string ClearConsole()
        {
            Console.Clear();
            return "";
        }
    }
}