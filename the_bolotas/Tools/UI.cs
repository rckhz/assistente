namespace the_bolotas.Tools;

public static class UI
{
    public static void WriteColor(string texto, ConsoleColor cor)
    {
        var anterior = Console.ForegroundColor;

        Console.ForegroundColor = cor;
        Console.Write(texto);
        Console.ForegroundColor = anterior;
    }
    public static bool Confirm(string pergunta)
    {
        WriteColor($"{pergunta} (s/n): ", ConsoleColor.Yellow);

        var resposta = Console.ReadLine();

        return resposta?.Trim().ToLower() == "s";
    }
}