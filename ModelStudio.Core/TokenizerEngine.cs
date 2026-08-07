using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelStudio.Core;

public class TokenInfo
{
    public int Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Type { get; set; } = "Normal";
}

public class TokenizerEngine
{
    public static List<TokenInfo> InspectTokens(GgufHeader header, string sampleText)
    {
        var result = new List<TokenInfo>();
        if (string.IsNullOrEmpty(sampleText)) return result;

        // Try extracting vocabulary from GGUF metadata
        var tokensObj = header.Metadata.TryGetValue("tokenizer.ggml.tokens", out var tVal) ? tVal as List<object> : null;
        var vocab = new List<string>();
        if (tokensObj != null)
        {
            foreach (var o in tokensObj) vocab.Add(o?.ToString() ?? "");
        }

        // Simple BPE / Wordpiece simulation for visualization
        var words = sampleText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        int tokenIdCounter = 1;

        foreach (var word in words)
        {
            int foundId = vocab.Count > 0 ? vocab.IndexOf(word) : -1;
            if (foundId >= 0)
            {
                result.Add(new TokenInfo { Id = foundId, Token = word, Type = "Exact Vocabulary Match" });
            }
            else
            {
                // Subword breakdown
                for (int i = 0; i < word.Length; i += 3)
                {
                    var sub = word.Substring(i, Math.Min(3, word.Length - i));
                    result.Add(new TokenInfo { Id = tokenIdCounter * 107 + i, Token = sub, Type = "Subword Token" });
                }
                tokenIdCounter++;
            }
        }

        return result;
    }
}
