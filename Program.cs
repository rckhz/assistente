using the_bolotas.Actions;
using the_bolotas.Api;
using the_bolotas.Core;
using the_bolotas.Memory;
using the_bolotas.Prompts;

namespace the_bolotas;

internal class Program
{
    static async Task Main()
    {
        var baseDir = AppContext.BaseDirectory;

        var config = Config.Load(baseDir);
        var history = new HistoryManager(baseDir);
        var client = new ClaudeClient(config);
        var executor = new ActionExecutor();

        while (true)
        {
            Console.Write("> ");

            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input == "sair")
                break;

            var prompt = MainPrompt.Build(
                input,
                Environment.UserName,
                history.FormatRecent(5),
                executor.PastaAtual
            );

            var aiResponse = await client.AskAsync(
                prompt,
                CancellationToken.None
            );

            var resultado = await executor.ExecuteAsync(
                aiResponse,
                CancellationToken.None
            );

            Console.WriteLine(resultado);

            history.Add(input, resultado);
        }

        history.Save();
    }
}