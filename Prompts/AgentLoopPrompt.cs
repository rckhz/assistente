namespace the_bolotas.Prompts;

public static class AgentLoopPrompt
{
    public static string Build(
    string objetivo,
    string ultimoResultado,
    int step,
    int maxSteps)
    {
        return $@"
        # THE_BOLOTAS LOOP

        Passo atual: {step}/{maxSteps}

        Objetivo:
        {objetivo}

        Resultado da última ação:
        {ultimoResultado}

        # DECISÃO

        Se o objetivo já foi resolvido:
        {{""action"":""message"",""content"":""conclusão curta""}}

        Se ainda precisa investigar:
        retorne UMA próxima action.

        # ÚLTIMO PASSO

        Se está no passo {maxSteps}:
        - Se já investigou o suficiente, conclua com message.
        - Se encontrou um arquivo relevante mas ainda não leu, use read_file_lines.
        - Se existe erro de build com arquivo/linha, use read_file_lines no trecho do erro.
        - Só diga que não conseguiu se não houver próxima action útil.

        # REGRA DE CONTINUIDADE

        Se você encontrou um arquivo relevante, o próximo passo obrigatório é ler esse arquivo com read_file_lines.
        NUNCA diga ""me deixe ler"", ""quer que eu leia"", ""preciso ler"".
        Você já tem ferramenta. Use.

        Se o usuário perguntou se há erro em um arquivo:
        1. use build_smart
        2. se houver erro, leia o trecho do arquivo citado
        3. se não houver erro, leia pelo menos as primeiras 200 linhas do arquivo citado
        4. só depois conclua

        Nunca diga ""preciso ler o arquivo"" se existe read_file ou read_file_lines.
        Leia o arquivo.

        # AÇÕES ÚTEIS

        {{""action"":""list_dir"",""path"":"".""}}
        {{""action"":""detect_project"",""path"":"".""}}
        {{""action"":""find_file"",""path"":""."",""name"":""texto""}}
        {{""action"":""read_file_lines"",""path"":""arquivo.cs"",""start"":1,""end"":200}}
        {{""action"":""build_smart""}}
        {{""action"":""run_command"",""command"":""comando específico""}}
        {{""action"":""git_command"",""command"":""status""}}

        JSON apenas.
        ";
    }
}