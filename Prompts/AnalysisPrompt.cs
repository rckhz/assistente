namespace the_bolotas.Prompts;

public static class AnalysisPrompt
{
    public static string Build(
        string objetivo,
        string resultado)
    {
        return $@"
        Você é o the_bolotas.

        Analise o resultado abaixo e responda de forma curta e direta.

        Objetivo:
        {objetivo}

        Resultado:
        {resultado}

        Explique:
        - O que aconteceu
        - Se houve erro
        - Qual a provável causa
        - O próximo passo recomendado

        Resposta em texto simples.
        ";
    }
}