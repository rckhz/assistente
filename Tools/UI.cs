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
}