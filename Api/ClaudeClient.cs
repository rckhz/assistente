using System.Net.Http;
using System.Text;
using System.Text.Json;
using the_bolotas.Core;
using the_bolotas.Tools;

namespace the_bolotas.Api;

public class ClaudeClient
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(Constants.HttpTimeoutSeconds)
    };

    private readonly Config _config;

    public ClaudeClient(Config config)
    {
        _config = config;
    }

    public async Task<string> AskAsync(string prompt, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new
        {
            model = _config.Model,
            prompt
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, _config.ApiUrl);
        req.Headers.Add("Authorization", $"Bearer {_config.ApiKey}");
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        HttpResponseMessage response;
        string responseText;

        try
        {
            response = await Http.SendAsync(req, ct);
            responseText = await response.Content.ReadAsStringAsync(ct);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new Exception("Timeout: a API demorou demais pra responder.");
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Falha de rede: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"API retornou {(int)response.StatusCode}: {Util.Truncar(responseText, 300)}"
            );
        }

        return ExtractTextFromSse(responseText);
    }

    private static string ExtractTextFromSse(string responseSse)
    {
        var sb = new StringBuilder();
        var foundSomething = false;

        foreach (var line in responseSse.Split('\n'))
        {
            var trimmed = line.Trim();

            if (!trimmed.StartsWith("data:"))
                continue;

            var payload = trimmed[5..].Trim();

            if (payload == "[DONE]" || string.IsNullOrEmpty(payload))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(payload);

                if (doc.RootElement.TryGetProperty("delta", out var delta))
                {
                    sb.Append(delta.GetString());
                    foundSomething = true;
                }
            }
            catch
            {
            }
        }

        return foundSomething ? sb.ToString() : responseSse;
    }
}