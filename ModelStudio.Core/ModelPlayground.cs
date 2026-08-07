using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ModelStudio.Core;

public class ModelPlayground
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(120) };

    public static async Task<(bool success, string reply)> GeneratePromptResponseAsync(string serverUrl, string modelName, string prompt, string systemPrompt = "")
    {
        if (string.IsNullOrWhiteSpace(serverUrl)) serverUrl = "http://127.0.0.1:11434"; // Default Ollama port
        serverUrl = serverUrl.TrimEnd('/');

        try
        {
            if (serverUrl.Contains("11434")) // Ollama API
            {
                var url = $"{serverUrl}/api/generate";
                var payload = new
                {
                    model = modelName,
                    prompt = prompt,
                    system = systemPrompt,
                    stream = false
                };

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await _http.PostAsync(url, content);

                if (resp.IsSuccessStatusCode)
                {
                    var resStr = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(resStr);
                    var responseText = doc.RootElement.GetProperty("response").GetString() ?? "";
                    return (true, responseText);
                }
                else
                {
                    return (false, $"Ollama HTTP Error {resp.StatusCode}");
                }
            }
            else // OpenAI / llama-server API
            {
                var url = $"{serverUrl}/v1/chat/completions";
                var payload = new
                {
                    model = modelName,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = prompt }
                    },
                    stream = false
                };

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await _http.PostAsync(url, content);

                if (resp.IsSuccessStatusCode)
                {
                    var resStr = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(resStr);
                    var choices = doc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var msg = choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
                        return (true, msg);
                    }
                }
                return (false, $"Inference server returned HTTP {resp.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Inference error: {ex.Message}. Make sure local server is running at {serverUrl}.");
        }
    }
}
