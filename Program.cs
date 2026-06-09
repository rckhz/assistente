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
        var executor = new ActionExecutor();
        var agent = new Agent(client, executor, history);
        while (true)
        {
            Console.Write("> ");

            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Trim().ToLower() == "sair")
                break;

            var resposta = await agent.ProcessAsync(input, CancellationToken.None);
            Console.WriteLine(resposta);
        }

        history.Save();
    }
}