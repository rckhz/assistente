using System.Diagnostics;
using System.Text;
using System.Text.Json;
using the_bolotas.Tools;

namespace the_bolotas.Actions;

public class ActionExecutor
{
    public string PastaAtual { get; private set; } = Directory.GetCurrentDirectory();

    public async Task<string> ExecuteAsync(string jsonResposta, CancellationToken ct)
    {
        try
        {
            var json = Util.LimparMarkdown(jsonResposta);

            using var doc = JsonDocument.Parse(json);
            return await ExecuteElementAsync(doc.RootElement, ct);
        }
        catch (JsonException ex)
        {
            return $"[JSON inválido da IA] {ex.Message}\nResposta: {Util.Truncar(jsonResposta, 500)}";
        }
    }

    private async Task<string> ExecuteElementAsync(JsonElement root, CancellationToken ct)
    {
        if (!root.TryGetProperty("action", out var actionProp))
            return "[JSON sem action]";

        var action = actionProp.GetString() ?? "";

        try
        {
            return action switch
            {
                "message" => root.GetProperty("content").GetString() ?? "",

                "list_dir" => ListDir(root),
                "create_file" => CreateFile(root),
                "read_file" => ReadFile(root),
                "create_dir" => CreateDir(root),
                "change_dir" => ChangeDir(root),
                "run_command" => await RunCommandAsync(root, ct),
                "sequence" => await SequenceAsync(root, ct),
                "powershell" => await PowerShellAsync(root, ct),
                "git_command" => await GitCommandAsync(root, ct),
                "open_vscode" => await OpenVSCodeAsync(root, ct),
                _ => $"[ação ainda não implementada: {action}]"
            };
        }
        catch (Exception ex)
        {
            return $"[erro em {action}: {ex.Message}]";
        }
    }

    private string ListDir(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? ".");
        if (!Directory.Exists(path))
            return $"Pasta não existe: {path}";

        var sb = new StringBuilder();
        sb.AppendLine($"Conteúdo de {path}:");

        foreach (var dir in Directory.GetDirectories(path))
            sb.AppendLine($"[DIR]  {Path.GetFileName(dir)}");

        foreach (var file in Directory.GetFiles(path))
            sb.AppendLine($"       {Path.GetFileName(file)}");

        return sb.ToString();
    }

    private string CreateFile(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? "");
        var content = el.TryGetProperty("content", out var c)
            ? c.GetString() ?? ""
            : "";

        File.WriteAllText(path, content);
        return $"Arquivo criado: {path}";
    }

    private string ReadFile(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? "");

        if (!File.Exists(path))
            return $"Arquivo não existe: {path}";

        return File.ReadAllText(path);
    }

    private string CreateDir(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? "");
        Directory.CreateDirectory(path);
        return $"Pasta criada: {path}";
    }

    private string ChangeDir(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? ".");

        if (!Directory.Exists(path))
            return $"Pasta não existe: {path}";

        Directory.SetCurrentDirectory(path);
        PastaAtual = Directory.GetCurrentDirectory();

        return $"Pasta atual: {PastaAtual}";
    }

    private async Task<string> SequenceAsync(JsonElement el, CancellationToken ct)
    {
        if (!el.TryGetProperty("steps", out var steps) || steps.ValueKind != JsonValueKind.Array)
            return "[sequence sem steps válido]";

        var sb = new StringBuilder();
        var i = 1;

        foreach (var step in steps.EnumerateArray())
        {
            ct.ThrowIfCancellationRequested();

            var result = await ExecuteElementAsync(step, ct);
            sb.AppendLine($"Step {i}: {result}");
            i++;
        }

        return sb.ToString();
    }

    private string ResolvePath(string path)
    {
        path = Environment.ExpandEnvironmentVariables(path);

        if (!Path.IsPathRooted(path))
            path = Path.Combine(PastaAtual, path);

        return Path.GetFullPath(path);
    }

    private async Task<string> RunCommandAsync(
    JsonElement el,
    CancellationToken ct)
    {
        var command = el.GetProperty("command").GetString() ?? "";

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            WorkingDirectory = PastaAtual,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        return string.IsNullOrWhiteSpace(stderr)
            ? stdout
            : $"{stdout}\n{stderr}";
    }

    private async Task<string> PowerShellAsync(
    JsonElement el,
    CancellationToken ct)
    {
        var command = el.GetProperty("command").GetString() ?? "";

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -Command \"{command}\"",
            WorkingDirectory = PastaAtual,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        return string.IsNullOrWhiteSpace(stderr)
            ? stdout
            : $"{stdout}\n{stderr}";
    }

    private Task<string> GitCommandAsync(JsonElement el, CancellationToken ct)
    {
        var command = el.GetProperty("command").GetString() ?? "status";
        return RunProcessAsync("git", command, ct);
    }

    private Task<string> OpenVSCodeAsync(JsonElement el, CancellationToken ct)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? ".");
        return RunProcessAsync("cmd.exe", $"/c code \"{path}\"", ct);
    }

    private async Task<string> RunProcessAsync(string exe, string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            WorkingDirectory = PastaAtual,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)!;

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        var stderr = await process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        if (string.IsNullOrWhiteSpace(stdout) && string.IsNullOrWhiteSpace(stderr))
            return $"processo finalizado: {exe} {args}";

        return string.IsNullOrWhiteSpace(stderr)
            ? stdout.TrimEnd()
            : $"{stdout.TrimEnd()}\n[stderr] {stderr.TrimEnd()}";
    }

}