namespace the_bolotas.Prompts;

public static class FixBuildPrompt
{
    public static string Build(string buildOutput, string code)
    {
        return $@"
        Você é um especialista em C#.

        Erro de build:

        {buildOutput}

        Trecho do código:

        {code}

        Responda SOMENTE JSON:

        {{
          ""action"":""preview_edit"",
          ""path"":""arquivo.cs"",
          ""old_content"":""trecho antigo"",
          ""new_content"":""trecho corrigido""
        }}

        Se não conseguir corrigir, use:

        {{
          ""action"":""message"",
          ""content"":""explicação""
        }}
        ";
    }
}