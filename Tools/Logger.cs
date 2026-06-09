using the_bolotas.Core;

namespace the_bolotas.Tools;

public static class Logger
{
    private static string _logPath = "";
    private static readonly object _lock = new();

    public static void Init(string baseDir)
    {
        _logPath = Path.Combine(baseDir, Constants.LogFileName);

        try
        {
            lock (_lock)
            {
                File.AppendAllText(
                    _logPath,
                    $"\n=== Sessão {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n"
                );
            }
        }
        catch (Exception ex)
        {
            UI.WriteColor($"[Falha ao iniciar log]: {ex.Message}\n", ConsoleColor.Red);
        }
    }

    public static void Log(string texto)
    {
        try
        {
            lock (_lock)
            {
                File.AppendAllText(
                    _logPath,
                    $"[{DateTime.Now:HH:mm:ss}] {texto}\n"
                );
            }
        }
        catch
        {
        }
    }

    public static string ReadRecent()
    {
        if (!File.Exists(_logPath))
            return "Sem log ainda.";

        string[] linhas;

        lock (_lock)
        {
            linhas = File.ReadAllLines(_logPath);
        }

        var recentes = linhas.Skip(
            Math.Max(0, linhas.Length - Constants.MaxLogLinesReturned)
        );

        var texto = string.Join("\n", recentes);

        if (texto.Length > Constants.MaxLogCharsReturned)
            texto = texto[^Constants.MaxLogCharsReturned..];

        return texto;
    }
}