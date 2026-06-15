using the_bolotas.Actions;
using the_bolotas.Api;
using the_bolotas.Memory;
using the_bolotas.Prompts;
using System.Text.Json;

namespace the_bolotas.Core;

public class Agent
{
    private const int MaxSteps = 50;
    private const int MaxResultLength = 1500;

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
            _executor.PastaAtual,
            _executor.GetMemoryFormatted());

        var respostaAtual = await _client.AskAsync(prompt, ct);
        var historicoLoop = new List<(string acao, string resultado)>();

        for (int step = 1; step <= MaxSteps; step++)
        {
            var resultado = await _executor.ExecuteAsync(respostaAtual, ct);
            var acaoResumo = ExtrairResumoAcao(respostaAtual);
            var resultadoTruncado = Truncar(resultado, MaxResultLength);

            historicoLoop.Add((acaoResumo, resultadoTruncado));


            if (EhMessageFinal(respostaAtual))
            {
                _history.Add(input, resultado);
                return resultado;
            }

            // Detector de loop: mesma ação 2x seguidas
            if (historicoLoop.Count >= 2 &&
                historicoLoop[^1].acao == historicoLoop[^2].acao &&
                historicoLoop[^1].acao != "[SISTEMA]")
            {
                historicoLoop.Add((
                    "[SISTEMA]",
                    "Você repetiu a mesma ação duas vezes seguidas. Mude de estratégia ou finalize com action:message."
                ));
            }

            if (step == MaxSteps)
                break;

            var proximoPrompt = AgentLoopPrompt.Build(
                input,
                usuario,
                _executor.PastaAtual,
                _executor.GetMemoryFormatted(),
                _history.FormatRecent(Constants.HistoryContextSize),
                historicoLoop,
                step + 1,
                MaxSteps);

            respostaAtual = await _client.AskAsync(proximoPrompt, ct);
        }

        // Atingiu o teto sem message — pede conclusão final
        var promptConclusao = AgentLoopPrompt.Build(
            input + "\n\n[Limite de passos atingido. Conclua AGORA com action:message resumindo o que descobriu até aqui.]",
            usuario,
            _executor.PastaAtual,
            _executor.GetMemoryFormatted(),
            _history.FormatRecent(Constants.HistoryContextSize),
            historicoLoop,
            MaxSteps,
            MaxSteps);

        var conclusao = await _client.AskAsync(promptConclusao, ct);
        var resultadoFinal = await _executor.ExecuteAsync(conclusao, ct);

        _history.Add(input, resultadoFinal);
        return resultadoFinal;
    }

    private static string ExtrairResumoAcao(string respostaJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(respostaJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("action", out var actionProp))
                return "(json inválido)";

            var action = actionProp.GetString() ?? "?";
            var partes = new List<string> { action };

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name == "action") continue;
                if (prop.Name == "content" || prop.Name == "new_content" || prop.Name == "old_content")
                {
                    partes.Add($"{prop.Name}=<...>");
                    continue;
                }
                if (prop.Name == "steps")
                {
                    partes.Add($"steps={prop.Value.GetArrayLength()}");
                    continue;
                }
                partes.Add($"{prop.Name}={prop.Value}");
            }

            return string.Join(" ", partes);
        }
        catch
        {
            return "(parse falhou)";
        }
    }

    private static bool EhMessageFinal(string respostaJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(respostaJson);
            return doc.RootElement.TryGetProperty("action", out var a)
                && a.GetString() == "message";
        }
        catch { return false; }
    }

    private static string Truncar(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        return s.Length <= max
            ? s
            : s.Substring(0, max) + $"\n... [truncado, {s.Length - max} chars omitidos]";
    }

    private bool IsCasualConversation(string input)
    {
        return false;
    }

    private Task<string> CasualReplyAsync(string input, CancellationToken ct)
        => _client.AskAsync(input, ct);
}
