# ⬡ ModelStudio IDE — Visual Architecture Inspector & Precision Model Tuner

[![Framework](https://img.shields.io/badge/.NET-10.0-8B5CF6?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-10B981?style=flat-square)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20%7C%20Linux%20%7C%20macOS-06B6D4?style=flat-square)](https://github.com/ash-forge/model-studio-ide)
[![Status](https://img.shields.io/badge/Status-v1.0.0--Pro-EC4899?style=flat-square)](https://github.com/ash-forge/model-studio-ide)

> **The Sovereign Visual IDE for AI Models & GGUF Quantization Architecture**
> *Inspect 400+ GB model weights in <309ms with zero RAM bloat, tune layer precision, edit Jinja2 chat templates, scale LoRAs, and publish to HuggingFace or Ollama in 1 click.*

---

## 🌟 Highlights & Features

- ⚡ **Sub-309ms Zero-Copy Memory Reader**: Native C# .NET 10 binary parser using `MemoryMappedFile` and `Span<T>`. Reads multi-hundred GB models in **309 milliseconds** with ~40MB RAM footprint.
- 🌐 **Universal Multi-Format Support**: GGUF v2/v3, SafeTensors, ONNX, and TFLite support across Gemma, Llama, Qwen, DeepSeek, and Mistral model families.
- 📝 **Interactive Header & Jinja2 Template Editor**: Dedicated multi-line Jinja2 chat template inspector (`tokenizer.chat_template`), RoPE frequency tuner, and metadata key-value editor.
- 🧩 **LoRA Adapter Workbench**: Inspect rank ($r$), alpha ($\alpha$), and target modules with live interactive alpha scaling sliders ($0.0\times \rightarrow 2.0\times$) and 1-click model merging.
- ⚡ **GGUF Precision Quantizer & Perplexity Evaluator**: Convert tensor precision between `IQ4_XS`, `Q4_K_M`, `Q6_K`, `Q8_0`, and `FP16` while calculating live perplexity SNR scores and Wikitext-2 quality grades.
- 💾 **Hardware VRAM Estimator**: Calculate exact GPU/TPU VRAM memory requirements for 4k, 32k, and 128k context windows.
- 🚀 **1-Click Publishing**: Direct 1-click deployment to **Ollama** (`ollama create`) and **HuggingFace Hub** repositories.
- 🔀 **FrankenMerge SLERP Merger & Splitter**: Slerp interpolation preview between Model A and Model B, plus multi-part GGUF file splitting.
- 🎨 **Dynamic Studio Theme Engine**: Sleek left navigation drawer sidebar with real-time themes (*Cyberpunk Neon*, *Obsidian Dark*, *Emerald Studio*, *Clean Dark Slate*).

---

## 🛠️ Installation & Building

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Quick Start & Build
```bash
git clone https://github.com/ash-forge/model-studio-ide.git
cd model-studio-ide/ModelStudio.App
dotnet build -c Release
```

---

## 📜 License & Acknowledgments

Built with ❤️ by **ash-forge © 2026**. Open source under the MIT License.
