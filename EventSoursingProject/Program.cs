using EventSoursingProject.Services;
using EventSoursingProject.Services.Storage;
using EventSoursingProject.Services.Storage.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.Title = "Event Sourcing Terminal";
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine(@"╔══════════════════════════════════════════════════════════╗");
Console.WriteLine(@"║     EVENT SOURCING ТЕРМИНАЛ                          ║");
Console.WriteLine(@"║     Управление счетами и операциями                  ║");
Console.WriteLine(@"╚══════════════════════════════════════════════════════════╝");
Console.ResetColor();
Console.WriteLine();

var services = new ServiceCollection();

services.AddSingleton<IOperationReader, JsonOperationReader>();
services.AddSingleton<IOperationWriter, JsonOperationWriter>();

services.AddSingleton<BalanceCalculator>();
services.AddSingleton<CommandExecutor>();

var serviceProvider = services.BuildServiceProvider();

var executor = serviceProvider.GetRequiredService<CommandExecutor>();

Console.WriteLine("Добро пожаловать в Event Sourcing терминал!");
Console.WriteLine("Введите 'help' для списка команд.");
Console.WriteLine();

while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("> ");
    Console.ResetColor();

    var input = Console.ReadLine()?.Trim() ?? "";

    if (input.ToLower() == "exit")
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n👋 До свидания!");
        Console.ResetColor();
        break;
    }

    try
    {
        var result = await executor.ExecuteAsync(input);

        if (result == "exit")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n👋 До свидания!");
            Console.ResetColor();
            break;
        }

        if (!string.IsNullOrEmpty(result))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(result);
            Console.ResetColor();
            Console.WriteLine();
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"❌ Ошибка: {ex.Message}");
        Console.ResetColor();
        Console.WriteLine();
    }
}