namespace the_bolotas.Tools;

public static class Util
{
    public static string Truncar(string s, int max)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        return s.Length <= max
            ? s
            : s[..max] + "...";
    }

    public static string LimparMarkdown(string s)
    {
        s = s.Trim();

        if (s.StartsWith("```"))
        {
            var idx = s.IndexOf('\n');

            if (idx > -1)
                s = s[(idx + 1)..];

            if (s.EndsWith("```"))
                s = s[..^3];

            s = s.Trim();
        }

        return s;
    }
}