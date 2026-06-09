using the_bolotas.Actions;
using the_bolotas.Api;
using the_bolotas.Memory;
using the_bolotas.Prompts;
using System.Text.Json;
namespace the_bolotas.Core;

public class Agent
{
    private const int MaxSteps = 8;
    private readonly ClaudeClient _client;
    private readonly ActionExecutor _executor;
    private readonly HistoryManager _history;

    public Agent(
        ClaudeClient client,
        ActionExecutor executor,
        HistoryManager history)
    {
        _client = client;
        _executor = executor;
        _history = history;
    }

    public async Task<string> ProcessAsync(
    string input,
    CancellationToken ct)
    {
        if (IsCasualConversation(input))
        {
            var resposta = await CasualReplyAsync(input, ct);
            _history.Add(input, resposta);
            return resposta;
        }
        var usuario = Environment.UserName;

        var prompt = MainPrompt.Build(
            input,
            usuario,
            _history.FormatRecent(Constants.HistoryContextSize),
            _executor.PastaAtual
        );

        var aiJson = await _client.AskAsync(prompt, ct);

        for (int step = 0; step < MaxSteps; step++)
        {
            var result = await _executor.ExecuteAsync(aiJson, ct);

            var continuationPrompt = AgentLoopPrompt.Build(
                input,
                result, 
                step + 1,
                MaxSteps
            );

            aiJson = await _client.AskAsync(continuationPrompt, ct);

            if (IsMessageAction(aiJson))
            {
                var finalResult = await _executor.ExecuteAsync(aiJson, ct);
                _history.Add(input, finalResult);
                return finalResult;
            }
        }

        _history.Add(input, "Limite de passos atingido.");
        return "parei porque bati o limite de passos. provavelmente fiquei repetindo investigação sem concluir.";
    }
    private static bool IsMessageAction(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json.Trim());
            return doc.RootElement.TryGetProperty("action", out var action)
                && action.GetString() == "message";
        }
        catch
        {
            return false;
        }
    }

    private static bool IsCasualConversation(string input)
    {
        var text = input.Trim().ToLowerInvariant();

        return text is "oi" or "olá" or "ola" or "eae" or "salve" or "fala"
            or "que" or "q" or "kkkk" or "kkk" or "mano";
    }

    private async Task<string> CasualReplyAsync(string input, CancellationToken ct)
    {
        var prompt = $@"
            Você é o the_bolotas.

            Responda como amigo dev brasileiro.
            Curto, natural, meio seco.
            Nada de suporte corporativo.
            Nada de 'como posso ajudar'.
            Nada de JSON.

            Usuário: {input}
            ";

        return await _client.AskAsync(prompt, ct);
    }
}