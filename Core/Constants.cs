namespace the_bolotas.Core;

public static class Constants
{
    public const string ApiUrl = "https://unlimited.surf/api/chat";
    public const string Model = "gateway-claude-opus-4-7";

    public const string LogFileName = "the_bolotas_log.txt";
    public const string HistoryFileName = "historico.json";
    public const string ConfigFileName = "config.json";

    public const int HttpTimeoutSeconds = 60;
    public const int MaxHistoryEntries = 50;
    public const int HistoryContextSize = 5;
    public const int MaxLogLinesReturned = 10000;
    public const int MaxLogCharsReturned = 4000;
    public const int MaxFileSizeForSearchBytes = 1_000_000;
    public const int MaxSearchMatches = 50;

    public static readonly string[] SkipDirsOnSearch =
    {
        "node_modules", ".git", "bin", "obj", "dist", ".vs", ".idea"
    };
}