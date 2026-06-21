using the_bolotas.Actions;
using the_bolotas.Api;
using the_bolotas.Core;
using the_bolotas.Memory;
using the_bolotas.Prompts;

namespace the_bolotas;

internal class Program
{
    [STAThread]
    static async Task Main()
    {
        var baseDir = AppContext.BaseDirectory;

        var config = Config.Load(baseDir);
        var history = new HistoryManager(baseDir);
        var client = new ClaudeClient(config);
        var executor = new ActionExecutor(client);
        var agent = new Agent(client, executor, history);
        while (true)
        {
            Console.WriteLine(
        "\n───────────────────────────────────────────────────────────────────────────────────");
            Console.Write("> ");
            var input = Console.ReadLine();
            Console.WriteLine(
       "────────────────────────────────────────────────────────────────────────────────────");

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Trim().ToLower() == "sair")
                break;

            try
            {
                var resposta = await agent.ProcessAsync(input, CancellationToken.None);

                Console.WriteLine();
                Console.WriteLine(resposta);
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"deu ruim: {ex.Message}");
                Console.WriteLine();
            }
        }

        history.Save();
    }
}