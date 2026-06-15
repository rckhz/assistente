using System.Text.Json;
using the_bolotas.Core;
using the_bolotas.Tools;

namespace the_bolotas.Memory;

public class Interacao
{
    public string Timestamp { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string Assistente { get; set; } = "";
}

public class HistoryManager
{
    private readonly string _path;
    private List<Interacao> _items = new();
    private int _changesSinceSave = 0;

    private const int SaveEvery = 5;

    public HistoryManager(string baseDir)
    {
        _path = Path.Combine(baseDir, Constants.HistoryFileName);
        Load();
    }

    public void Add(string userInput, string assistantResponse)
    {
        _items.Add(new Interacao
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Usuario = userInput,
            Assistente = assistantResponse
        });

        if (_items.Count > Constants.MaxHistoryEntries)
            _items = _items.Skip(_items.Count - Constants.MaxHistoryEntries).ToList();

        _changesSinceSave++;

        if (_changesSinceSave >= SaveEvery)
            Save();
    }

    public string FormatRecent(int n)
    {
        var ultimas = _items
            .TakeLast(n)
            .Select(h =>
                $"[{h.Timestamp}] U: {h.Usuario} | A: {Util.Truncar(h.Assistente, 200)}"
            );

        return string.Join("\n", ultimas);
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_path))
                return;

            var json = File.ReadAllText(_path);
            var dados = JsonSerializer.Deserialize<List<Interacao>>(json);

            if (dados != null)
                _items = dados;
        }
        catch (Exception ex)
        {
            UI.WriteColor($"[Falha ao carregar histórico]: {ex.Message}\n", ConsoleColor.Red);
            _items = new List<Interacao>();
        }
    }

    public void Save()
    {
        try
        {
            var tmp = _path + ".tmp";

            var json = JsonSerializer.Serialize(
                _items,
                new JsonSerializerOptions { WriteIndented = true }
            );

            File.WriteAllText(tmp, json);
            File.Move(tmp, _path, overwrite: true);

            _changesSinceSave = 0;
        }
        catch (Exception ex)
        {
            UI.WriteColor($"[Falha ao salvar histórico]: {ex.Message}\n", ConsoleColor.Red);
        }
    }
}