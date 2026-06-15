namespace the_bolotas.Prompts;

public static class FixCodePrompt
{
    public static string Build(string buildError, string code, string file)
    {
        return $@"
        Você é especialista em C#.

        Erro de build:
        {buildError}

        Arquivo:
        {file}

        Trecho do código:
        {code}

        Responda SOMENTE JSON.

        Se souber corrigir:
        {{""action"":""patch_file"",""path"":""{file}"",""old_content"":""trecho exato antigo"",""new_content"":""trecho corrigido"",""preview"":true}}

        Se não souber:
        {{""action"":""message"",""content"":""não consegui gerar patch seguro""}}
        ";
    }
}