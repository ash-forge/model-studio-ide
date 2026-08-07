using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ModelStudio.Core;

public class HuggingFaceUploader
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromHours(2) };

    public static async Task<(bool success, string message)> UploadModelAsync(string filePath, string repoId, string token, string commitMessage = "Upload model via ModelStudio IDE")
    {
        if (!File.Exists(filePath)) return (false, $"File not found: {filePath}");
        if (string.IsNullOrWhiteSpace(repoId)) return (false, "HuggingFace Repo ID is required (e.g., 'username/custom-model-repo').");
        if (string.IsNullOrWhiteSpace(token)) return (false, "HuggingFace API Token is required.");

        var fileName = Path.GetFileName(filePath);
        var url = $"https://huggingface.co/api/models/{repoId}/upload/main/{fileName}";

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());

            using var fileStream = File.OpenRead(filePath);
            using var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            req.Content = streamContent;

            using var resp = await _http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                return (true, $"Successfully uploaded '{fileName}' to HuggingFace repo '{repoId}'!\nURL: https://huggingface.co/{repoId}");
            }
            else
            {
                var errStr = await resp.Content.ReadAsStringAsync();
                return (false, $"HuggingFace API upload failed (HTTP {resp.StatusCode}):\n{errStr}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Upload error: {ex.Message}");
        }
    }
}
