using System.Text;

namespace the_bolotas.Prompts;

public static class AgentLoopPrompt
{
    public static string Build(
        string pedidoOriginal,
        string usuario,
        string pastaAtual,
        string memoria,
        string historicoConversa,
        IReadOnlyList<(string acao, string resultado)> historicoLoop,
        int proximoStep,
        int maxSteps)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# LOOP DE AGENTE - passo {proximoStep}/{maxSteps}");
        sb.AppendLine();

        if (proximoStep > maxSteps - 5)
        {
            sb.AppendLine($"ATENCAO: poucos passos restantes. Finalize JA com action:message resumindo o estado atual. Nao comece subtarefa nova.");
            sb.AppendLine();
        }

        sb.AppendLine("# HISTORICO DESTE LOOP");
        sb.AppendLine();

        if (historicoLoop.Count == 0)
        {
            sb.AppendLine("(nenhuma acao ainda)");
        }
        else
        {
            for (int i = 0; i < historicoLoop.Count; i++)
            {
                sb.AppendLine($"[passo {i + 1}]");
                sb.AppendLine($"  acao:      {historicoLoop[i].acao}");
                sb.AppendLine($"  resultado: {historicoLoop[i].resultado}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("Continue executando o pedido. Quando concluir, finalize com action:message.");
        sb.AppendLine("Se repetir a mesma acao sem progresso, mude de estrategia.");

        var pedidoComLoop = $"{pedidoOriginal}\n\n{sb}";

        return MainPrompt.Build(
            pedidoComLoop,
            usuario,
            historicoConversa,
            pastaAtual,
            memoria);
    }
}
