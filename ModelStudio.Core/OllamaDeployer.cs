using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ModelStudio.Core;

public class OllamaDeployer
{
    public static async Task<(bool success, string output)> DeployToOllamaAsync(string ggufFilePath, string modelName, string systemPrompt = "", int contextLength = 131072)
    {
        if (!File.Exists(ggufFilePath))
        {
            return (false, $"GGUF model file not found: {ggufFilePath}");
        }

        var modelfilePath = Path.Combine(Path.GetDirectoryName(ggufFilePath) ?? ".", $"Modelfile_{modelName}");
        var sb = new StringBuilder();
        sb.AppendLine($"FROM \"{ggufFilePath.Replace("\\", "/")}\"");
        sb.AppendLine($"PARAMETER num_ctx {contextLength}");
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            sb.AppendLine($"SYSTEM \"\"\"{systemPrompt}\"\"\"");
        }

        await File.WriteAllTextAsync(modelfilePath, sb.ToString(), Encoding.UTF8);

        var psi = new ProcessStartInfo
        {
            FileName = "ollama",
            Arguments = $"create {modelName} -f \"{modelfilePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using var proc = new Process { StartInfo = psi };
            proc.Start();

            var outTask = proc.StandardOutput.ReadToEndAsync();
            var errTask = proc.StandardError.ReadToEndAsync();

            await proc.WaitForExitAsync();

            var stdout = await outTask;
            var stderr = await errTask;

            if (proc.ExitCode == 0)
            {
                return (true, $"Successfully registered model '{modelName}' in Ollama!\n{stdout}");
            }
            else
            {
                return (false, $"Ollama registration failed (exit code {proc.ExitCode}):\n{stderr}\n{stdout}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Failed to invoke Ollama CLI: {ex.Message}. Make sure 'ollama' is installed and running.");
        }
    }
}
