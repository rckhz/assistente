using System.Text;

namespace the_bolotas.Memory;

public class MemoryManager
{
    private const int MaxBytesPerFile = 2048;
    private static readonly string[] ValidFiles = { "project", "preferences", "notes" };

    private readonly string _baseDir;

    public MemoryManager(string projectRoot)
    {
        _baseDir = Path.Combine(projectRoot, ".bolotas");
    }

    public string LoadFormatted()
    {
        if (!Directory.Exists(_baseDir))
            return "(memória vazia)";

        var sb = new StringBuilder();
        var any = false;

        foreach (var name in ValidFiles)
        {
            var path = Path.Combine(_baseDir, name + ".md");
            if (!File.Exists(path)) continue;

            var content = File.ReadAllText(path).Trim();
            if (string.IsNullOrWhiteSpace(content)) continue;

            sb.AppendLine($"## {name}");
            sb.AppendLine(content);
            sb.AppendLine();
            any = true;
        }

        return any ? sb.ToString().TrimEnd() : "(memória vazia)";
    }

    public string Append(string file, string content)
    {
        if (Array.IndexOf(ValidFiles, file) < 0)
            return $"[memória] arquivo inválido: {file}. Use: project, preferences, notes";

        if (string.IsNullOrWhiteSpace(content))
            return "[memória] conteúdo vazio";

        Directory.CreateDirectory(_baseDir);
        EnsureGitignore();

        var path = Path.Combine(_baseDir, file + ".md");
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        var entry = $"[{timestamp}] {content.Trim()}";

        File.AppendAllText(path, entry + Environment.NewLine);
        Truncate(path);

        return $"[memória] salvo em {file}.md";
    }

    private void Truncate(string path)
    {
        var info = new FileInfo(path);
        if (info.Length <= MaxBytesPerFile) return;

        var lines = File.ReadAllLines(path).ToList();
        while (lines.Count > 0)
        {
            var totalBytes = lines.Sum(l => Encoding.UTF8.GetByteCount(l) + 1);
            if (totalBytes <= MaxBytesPerFile) break;
            lines.RemoveAt(0);
        }

        File.WriteAllLines(path, lines);
    }

    private void EnsureGitignore()
    {
        var gitignore = Path.Combine(_baseDir, ".gitignore");
        if (File.Exists(gitignore)) return;

        File.WriteAllText(gitignore, "# memória local do the_bolotas\n*\n");
    }
}
