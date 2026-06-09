using System.Text.Json;
using the_bolotas.Tools;

namespace the_bolotas.Core;

public class Config
{
    public string ApiKey { get; set; } = "";
    public string ApiUrl { get; set; } = Constants.ApiUrl;
    public string Model { get; set; } = Constants.Model;

    public static Config Load(string basePath)
    {
        var envKey = Environment.GetEnvironmentVariable("THE_BOLOTAS_API_KEY");
        var configPath = Path.Combine(basePath, Constants.ConfigFileName);

        var cfg = new Config();

        if (File.Exists(configPath))
        {
            try
            {
                var raw = File.ReadAllText(configPath);

                var opts = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                cfg = JsonSerializer.Deserialize<Config>(raw, opts) ?? new Config();
            }
            catch (Exception ex)
            {
                UI.WriteColor($"[Falha ao ler config.json]: {ex.Message}\n", ConsoleColor.Red);
            }
        }

        if (!string.IsNullOrWhiteSpace(envKey))
            cfg.ApiKey = envKey;

        if (string.IsNullOrWhiteSpace(cfg.ApiKey))
            throw new Exception("API key não definida. Use THE_BOLOTAS_API_KEY ou config.json.");

        return cfg;
    }
}