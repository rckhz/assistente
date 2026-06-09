using System.Diagnostics;
using System.Text;
using System.Text.Json;
using the_bolotas.Tools;
using System.Windows.Forms;
namespace the_bolotas.Actions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using the_bolotas.Tools;
public class ActionExecutor
{
    public string PastaAtual { get; private set; } = FindProjectRoot();

    private static string FindProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Any() || dir.GetFiles("*.sln").Any())
                return dir.FullName;

            dir = dir.Parent;
        }

        return Directory.GetCurrentDirectory();
    }

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
                "search_in_files" => SearchInFiles(root),
                "git_command" => await GitCommandAsync(root, ct),
                "open_vscode" => await OpenVSCodeAsync(root, ct),
                "copy_file" => CopyFile(root),
                "move_file" => MoveFile(root),
                "delete_file" => DeleteFile(root),
                "delete_dir" => DeleteDir(root),
                "kill_process" => KillProcess(root),
                "read_file_lines" => ReadFileLines(root),
                "append_file" => AppendFile(root),
                "detect_project" => DetectProject(root),
                "git_commit" => await GitCommitAsync(root, ct),
                "find_file" => FindFile(root),
                "diff_files" => DiffFiles(root),
                "read_clipboard" => ReadClipboard(),
                "write_clipboard" => WriteClipboard(root),
                "run_tests" => await RunTestsAsync(ct),
                "preview_edit" => PreviewEdit(root),
                "patch_file" => PatchFile(root),
                "replace_regex" => ReplaceRegex(root),
                "build_smart" => await BuildSmartAsync(ct),
                "test_smart" => await TestSmartAsync(ct),
                "fix_build" => await FixBuildAsync(ct),
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

    private string SearchInFiles(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? ".");
        var query = el.GetProperty("query").GetString() ?? "";
        var extension = el.TryGetProperty("extension", out var ext)
            ? ext.GetString() ?? ""
            : "";

        if (!Directory.Exists(path))
            return $"Pasta não existe: {path}";

        var matches = new List<string>();
        var pattern = string.IsNullOrWhiteSpace(extension)
            ? "*.*"
            : $"*{extension}";

        foreach (var file in Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories))
        {
            if (matches.Count >= 50)
                break;

            if (file.Contains("\\bin\\") || file.Contains("\\obj\\") || file.Contains("\\.git\\"))
                continue;

            try
            {
                var text = File.ReadAllText(file);

                if (text.Contains(query, StringComparison.OrdinalIgnoreCase))
                    matches.Add(file);
            }
            catch
            {
            }
        }

        if (matches.Count == 0)
            return $"Nada encontrado para: {query}";

        return $"Arquivos encontrados:\n" + string.Join("\n", matches);
    }
    private string CopyFile(JsonElement el)
    {
        var from = ResolvePath(el.GetProperty("from").GetString() ?? "");
        var to = ResolvePath(el.GetProperty("to").GetString() ?? "");

        File.Copy(from, to, true);

        return $"Copiado: {from} -> {to}";
    }
    private string MoveFile(JsonElement el)
    {
        var from = ResolvePath(el.GetProperty("from").GetString() ?? "");
        var to = ResolvePath(el.GetProperty("to").GetString() ?? "");

        File.Move(from, to, true);

        return $"Movido: {from} -> {to}";
    }

    private string DeleteFile(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? "");

        if (!File.Exists(path))
            return $"Arquivo não existe: {path}";

        if (!UI.Confirm($"Deletar arquivo '{path}'?"))
            return "Operação cancelada.";

        File.Delete(path);

        return $"Arquivo deletado: {path}";
    }

    private string DeleteDir(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? "");

        if (!Directory.Exists(path))
            return $"Pasta não existe: {path}";

        if (!UI.Confirm($"Deletar pasta '{path}' e tudo dentro dela?"))
            return "Operação cancelada.";

        Directory.Delete(path, true);

        return $"Pasta deletada: {path}";
    }

    private string KillProcess(JsonElement el)
    {
        if (el.TryGetProperty("pid", out var pidProp))
        {
            var pid = pidProp.GetInt32();

            var process = Process.GetProcessById(pid);

            var nome = process.ProcessName;

            process.Kill(true);
            process.WaitForExit();

            return $"Processo encerrado: {nome} ({pid})";
        }

        if (el.TryGetProperty("name", out var nameProp))
        {
            var name = nameProp.GetString() ?? "";

            var processes = Process.GetProcessesByName(name);

            if (processes.Length == 0)
                return $"Nenhum processo encontrado: {name}";

            foreach (var process in processes)
            {
                process.Kill(true);
                process.WaitForExit();
            }

            return $"Encerrados {processes.Length} processo(s) '{name}'";
        }

        return "kill_process requer pid ou name";
    }

    private string ReadFileLines(string path, int start, int end)
    {
        if (!File.Exists(path))
            return $"Arquivo não existe: {path}";

        var lines = File.ReadAllLines(path);

        start = Math.Max(1, start);
        end = Math.Min(lines.Length, end);

        if (start > end)
            return "Intervalo inválido.";

        var sb = new StringBuilder();

        for (int i = start - 1; i < end; i++)
            sb.AppendLine($"{i + 1}: {lines[i]}");

        return sb.ToString();
    }
    private string ReadFileLines(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? "");
        var start = el.GetProperty("start").GetInt32();
        var end = el.GetProperty("end").GetInt32();

        return ReadFileLines(path, start, end);
    }
    private string AppendFile(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? "");
        var content = el.GetProperty("content").GetString() ?? "";

        File.AppendAllText(path, content + Environment.NewLine);

        return $"Conteúdo adicionado em: {path}";
    }
    private string DetectProject(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? ".");

        if (!Directory.Exists(path))
            return $"Pasta não existe: {path}";

        if (Directory.GetFiles(path, "*.csproj").Any())
            return "Projeto detectado: .NET\nBuild: dotnet build\nRun: dotnet run\nTest: dotnet test";

        if (File.Exists(Path.Combine(path, "package.json")))
            return "Projeto detectado: Node.js\nBuild: npm run build\nRun: npm start\nTest: npm test";

        if (File.Exists(Path.Combine(path, "requirements.txt")) ||
            File.Exists(Path.Combine(path, "pyproject.toml")))
            return "Projeto detectado: Python\nRun: python main.py\nTest: pytest";

        return "Tipo de projeto não detectado.";
    }
    private async Task<string> GitCommitAsync(JsonElement el, CancellationToken ct)
    {
        var message = el.GetProperty("message").GetString() ?? "update";

        var add = await RunProcessAsync("git", "add .", ct);
        var commit = await RunProcessAsync("git", $"commit -m \"{message}\"", ct);

        return add + "\n" + commit;
    }
    private string FindFile(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? ".");
        var name = el.GetProperty("name").GetString() ?? "";

        if (!Directory.Exists(path))
            return $"Pasta não existe: {path}";

        var results = new List<string>();

        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            if (results.Count >= 50)
                break;

            if (file.Contains("\\bin\\") ||
                file.Contains("\\obj\\") ||
                file.Contains("\\.git\\") ||
                file.Contains("\\node_modules\\"))
                continue;

            var fileName = Path.GetFileName(file);

            if (fileName.Contains(name, StringComparison.OrdinalIgnoreCase))
                results.Add(file);
        }

        if (results.Count == 0)
            return $"Nenhum arquivo encontrado com: {name}";

        return "Arquivos encontrados:\n" + string.Join("\n", results);
    }
    private string DiffFiles(JsonElement el)
    {
        var from = ResolvePath(el.GetProperty("from").GetString() ?? "");
        var to = ResolvePath(el.GetProperty("to").GetString() ?? "");

        if (!File.Exists(from))
            return $"Arquivo não existe: {from}";

        if (!File.Exists(to))
            return $"Arquivo não existe: {to}";

        var a = File.ReadAllLines(from);
        var b = File.ReadAllLines(to);

        var sb = new StringBuilder();
        sb.AppendLine($"Diff: {from} -> {to}");

        var max = Math.Max(a.Length, b.Length);
        var changes = 0;

        for (int i = 0; i < max; i++)
        {
            var left = i < a.Length ? a[i] : null;
            var right = i < b.Length ? b[i] : null;

            if (left == right)
                continue;

            changes++;

            sb.AppendLine($"Linha {i + 1}:");

            if (left != null)
                sb.AppendLine($"- {left}");

            if (right != null)
                sb.AppendLine($"+ {right}");

            if (changes >= 100)
            {
                sb.AppendLine("[diff truncado: mais de 100 alterações]");
                break;
            }
        }

        if (changes == 0)
            return "Arquivos iguais.";

        return sb.ToString();
    }
    private string ReadClipboard()
    {
        return RunSta(() =>
        {
            if (!Clipboard.ContainsText())
                return "Clipboard vazio ou sem texto.";

            return Clipboard.GetText();
        });
    }

    private string WriteClipboard(JsonElement el)
    {
        var content = el.GetProperty("content").GetString() ?? "";

        return RunSta(() =>
        {
            Clipboard.SetText(content);
            return "Copiado para a área de transferência.";
        });
    }

    private static T RunSta<T>(Func<T> action)
    {
        T? result = default;
        Exception? error = null;

        var thread = new Thread(() =>
        {
            try { result = action(); }
            catch (Exception ex) { error = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error != null)
            throw error;

        return result!;
    }
    private async Task<string> RunTestsAsync(CancellationToken ct)
    {
        var projectType = DetectProjectType(PastaAtual);

        return projectType switch
        {
            "dotnet" => await RunProcessAsync("dotnet", "test", ct),
            "node" => await RunProcessAsync("npm", "test", ct),
            "python" => await RunProcessAsync("pytest", "", ct),
            _ => "Tipo de projeto não suportado."
        };
    }

    private string DetectProjectType(string path)
    {
        if (Directory.GetFiles(path, "*.csproj").Any())
            return "dotnet";

        if (File.Exists(Path.Combine(path, "package.json")))
            return "node";

        if (File.Exists(Path.Combine(path, "requirements.txt")) ||
            File.Exists(Path.Combine(path, "pyproject.toml")))
            return "python";

        return "unknown";
    }
    private string PreviewEdit(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? "");
        var oldContent = el.GetProperty("old_content").GetString() ?? "";
        var newContent = el.GetProperty("new_content").GetString() ?? "";

                return $"""
        Arquivo: {path}

        --- ANTES ---
        {oldContent}

        --- DEPOIS ---
        {newContent}
        """;
    }
    private string PatchFile(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? "");
        var oldContent = el.GetProperty("old_content").GetString() ?? "";
        var newContent = el.GetProperty("new_content").GetString() ?? "";

        var preview =
            el.TryGetProperty("preview", out var previewProp)
            && previewProp.GetBoolean();

        if (!File.Exists(path))
            return $"Arquivo não existe: {path}";

        var text = File.ReadAllText(path);

        if (!text.Contains(oldContent))
            return "Trecho antigo não encontrado.";

        if (preview)
        {
            return $"""
        Arquivo: {path}

        --- ANTES ---
        {oldContent}

        --- DEPOIS ---
        {newContent}
        """;
        }

        text = text.Replace(oldContent, newContent);

        File.WriteAllText(path, text);

        return $"Patch aplicado: {path}";
    }
    private string ReplaceRegex(JsonElement el)
    {
        var path = ResolvePath(el.GetProperty("path").GetString() ?? "");
        var pattern = el.GetProperty("pattern").GetString() ?? "";
        var replacement = el.GetProperty("replacement").GetString() ?? "";

        var preview =
            el.TryGetProperty("preview", out var previewProp)
            && previewProp.GetBoolean();

        if (!File.Exists(path))
            return $"Arquivo não existe: {path}";

        var text = File.ReadAllText(path);

        var regex = new Regex(
            pattern,
            RegexOptions.Multiline);

        var matches = regex.Matches(text);

        if (matches.Count == 0)
            return "Nenhuma ocorrência encontrada.";

        var newText = regex.Replace(text, replacement);

        if (preview)
        {
            return $"""
        Encontradas: {matches.Count} ocorrência(s)

        Pattern:
        {pattern}

        Replacement:
        {replacement}

        Prévia criada. Nenhuma alteração aplicada.
        """;
        }

        File.WriteAllText(path, newText);

        return $"Regex aplicado. Alteradas {matches.Count} ocorrência(s).";
    }

    private async Task<string> BuildSmartAsync(CancellationToken ct)
    {
        var type = DetectProjectType(PastaAtual);

        return type switch
        {
            "dotnet" => await RunProcessAsync("dotnet", "build", ct),
            "node" => await RunProcessAsync("npm", "run build", ct),
            "python" => "Projetos Python normalmente não possuem build.",
            _ => "Tipo de projeto não detectado."
        };
    }
    private async Task<string> TestSmartAsync(CancellationToken ct)
    {
        var type = DetectProjectType(PastaAtual);

        return type switch
        {
            "dotnet" => await RunProcessAsync("dotnet", "test", ct),
            "node" => await RunProcessAsync("npm", "test", ct),
            "python" => await RunProcessAsync("pytest", "", ct),
            _ => "Tipo de projeto não detectado."
        };
    }
    private async Task<string> FixBuildAsync(CancellationToken ct)
    {
        var buildResult = await BuildSmartAsync(ct);

        var error = BuildErrorParser.Parse(buildResult);
        var start = Math.Max(1, error.Line - 10);
        var end = error.Line + 10;
        var code = ReadFileLines(error.File, start, end);
        if (error == null)
            return buildResult;

        return $"""
        Erro encontrado

        Código: {error.Code}
        Arquivo: {error.File}
        Linha: {error.Line}

        {error.Message}
        """;
    }
}

