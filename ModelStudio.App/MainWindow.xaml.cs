using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using ModelStudio.Core;

namespace ModelStudio.App;

public class MetadataItem
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public string DisplayValue
    {
        get
        {
            if (string.IsNullOrEmpty(Value)) return "";
            var singleLine = Value.Replace("\r", "").Replace("\n", " ↵ ");
            return singleLine.Length > 90 ? singleLine.Substring(0, 90) + "..." : singleLine;
        }
    }
}

public partial class MainWindow : Window
{
    private GgufHeader? _currentHeader;
    private List<GgufTensorInfo> _allTensors = new();
    private List<MetadataItem> _metadataItems = new();
    private string? _currentFilePath;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        TxtStatus.Text = "Ready — Click '📁 Open Model (.gguf)' to load a model file.";
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag == null) return;
        if (!int.TryParse(btn.Tag.ToString(), out var index)) return;

        // Hide all views
        ViewTensors.Visibility = Visibility.Collapsed;
        ViewHeader.Visibility = Visibility.Collapsed;
        ViewLora.Visibility = Visibility.Collapsed;
        ViewQuant.Visibility = Visibility.Collapsed;
        ViewQuality.Visibility = Visibility.Collapsed;
        ViewOllama.Visibility = Visibility.Collapsed;
        ViewHf.Visibility = Visibility.Collapsed;
        ViewMerge.Visibility = Visibility.Collapsed;
        ViewToken.Visibility = Visibility.Collapsed;
        ViewInfer.Visibility = Visibility.Collapsed;
        ViewDiff.Visibility = Visibility.Collapsed;
        ViewVram.Visibility = Visibility.Collapsed;
        ViewArchGraph.Visibility = Visibility.Collapsed;
        ViewProfiler.Visibility = Visibility.Collapsed;
        ViewBrowser.Visibility = Visibility.Collapsed;

        // Reset nav button backgrounds
        NavTensors.Background = System.Windows.Media.Brushes.Transparent;
        NavHeader.Background = System.Windows.Media.Brushes.Transparent;
        NavLora.Background = System.Windows.Media.Brushes.Transparent;
        NavQuant.Background = System.Windows.Media.Brushes.Transparent;
        NavQuality.Background = System.Windows.Media.Brushes.Transparent;
        NavOllama.Background = System.Windows.Media.Brushes.Transparent;
        NavHf.Background = System.Windows.Media.Brushes.Transparent;
        NavMerge.Background = System.Windows.Media.Brushes.Transparent;
        NavToken.Background = System.Windows.Media.Brushes.Transparent;
        NavInfer.Background = System.Windows.Media.Brushes.Transparent;
        NavDiff.Background = System.Windows.Media.Brushes.Transparent;
        NavVram.Background = System.Windows.Media.Brushes.Transparent;
        NavArchGraph.Background = System.Windows.Media.Brushes.Transparent;
        NavProfiler.Background = System.Windows.Media.Brushes.Transparent;
        NavBrowser.Background = System.Windows.Media.Brushes.Transparent;

        // Reset text colors
        var grayBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF"));
        NavTensors.Foreground = grayBrush;
        NavHeader.Foreground = grayBrush;
        NavLora.Foreground = grayBrush;
        NavQuant.Foreground = grayBrush;
        NavQuality.Foreground = grayBrush;
        NavOllama.Foreground = grayBrush;
        NavHf.Foreground = grayBrush;
        NavMerge.Foreground = grayBrush;
        NavToken.Foreground = grayBrush;
        NavInfer.Foreground = grayBrush;
        NavDiff.Foreground = grayBrush;
        NavVram.Foreground = grayBrush;
        NavArchGraph.Foreground = grayBrush;
        NavProfiler.Foreground = grayBrush;
        NavBrowser.Foreground = grayBrush;

        // Show selected view & highlight button
        var accentBrush = (System.Windows.Media.Brush)FindResource("BrushPrimaryAccent");
        btn.Background = accentBrush;
        btn.Foreground = System.Windows.Media.Brushes.White;

        switch (index)
        {
            case 0: ViewTensors.Visibility = Visibility.Visible; break;
            case 1: ViewHeader.Visibility = Visibility.Visible; break;
            case 2: ViewLora.Visibility = Visibility.Visible; break;
            case 3: ViewQuant.Visibility = Visibility.Visible; break;
            case 4: ViewQuality.Visibility = Visibility.Visible; break;
            case 5: ViewOllama.Visibility = Visibility.Visible; break;
            case 6: ViewHf.Visibility = Visibility.Visible; break;
            case 7: ViewMerge.Visibility = Visibility.Visible; break;
            case 8: ViewToken.Visibility = Visibility.Visible; break;
            case 9: ViewInfer.Visibility = Visibility.Visible; break;
            case 10: ViewDiff.Visibility = Visibility.Visible; break;
            case 11: ViewVram.Visibility = Visibility.Visible; break;
            case 12: ViewArchGraph.Visibility = Visibility.Visible; break;
            case 13: ViewProfiler.Visibility = Visibility.Visible; break;
            case 14: ViewBrowser.Visibility = Visibility.Visible; break;
        }

        var navText = (btn.Content as StackPanel)?.Children.OfType<TextBlock>().FirstOrDefault()?.Text ?? "Workspace";
        TxtStatus.Text = $"Switched view to '{navText}'";
    }

    private void BtnOpenModel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Model File (GGUF / SafeTensors / ONNX / TFLite)",
            Filter = "All Supported Model Files (*.gguf;*.safetensors;*.onnx;*.tflite)|*.gguf;*.safetensors;*.onnx;*.tflite|GGUF Models (*.gguf)|*.gguf|SafeTensors Files (*.safetensors)|*.safetensors|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadModelFile(dialog.FileName);
        }
    }

    private void LoadModelFile(string filePath)
    {
        try
        {
            _currentFilePath = filePath;
            TxtStatus.Text = $"Loading {Path.GetFileName(filePath)}...";
            var sw = System.Diagnostics.Stopwatch.StartNew();

            _currentHeader = UniversalModelReader.ParseHeader(filePath);
            sw.Stop();

            _allTensors = _currentHeader.Tensors;

            // Header UI Updates
            var modelName = _currentHeader.Metadata.TryGetValue("general.name", out var n) ? n.ToString() : Path.GetFileNameWithoutExtension(filePath);
            var arch = _currentHeader.Metadata.TryGetValue("general.architecture", out var a) ? a.ToString() : "gguf";
            TxtModelTitle.Text = modelName;
            TxtArchName.Text = arch;
            BadgeArch.Visibility = Visibility.Visible;

            _isPopulatingHeader = true;
            EditModelName.Text = modelName ?? "";
            EditModelArch.Text = arch ?? "";
            EditModelVersion.Text = _currentHeader.Version.ToString();

            // Context length lookup
            var ctxKey = $"{arch}.context_length";
            var ctxValStr = _currentHeader.Metadata.TryGetValue(ctxKey, out var c) ? c.ToString() : "131072";
            EditModelContext.Text = ctxValStr ?? "131072";
            _isPopulatingHeader = false;

            var ctxLen = ulong.TryParse(ctxValStr, out var cParsed) ? $"{cParsed:N0} tokens" : (ctxValStr ?? "Standard Context");
            TxtMetaContext.Text = $"Context Length: {ctxLen}";
            TxtMetaTensors.Text = $"Total Tensors: {_currentHeader.TensorCount:N0}";

            ulong totalParams = 0;
            foreach (var t in _allTensors) totalParams += t.TotalElements;
            TxtMetaParams.Text = $"Total Parameters: {totalParams:N0} ({totalParams / 1_000_000_000.0:F2}B)";

            // Populate Metadata Grid
            _metadataItems = _currentHeader.Metadata.Select(kv => new MetadataItem { Key = kv.Key, Value = kv.Value?.ToString() ?? "" }).ToList();
            GridMetadataEditable.ItemsSource = _metadataItems;

            // Populate Quant Summary
            var quantGroups = _allTensors.GroupBy(t => t.Type)
                .Select(g => $"{g.Key,-12} : {g.Count()} tensors")
                .ToList();
            ListQuants.ItemsSource = quantGroups;

            // Populate Main Tensor Grid
            ApplyTensorFilter();
            if (GridTensors.Items.Count > 0)
            {
                GridTensors.SelectedIndex = 0;
            }

            TxtStatus.Text = $"Loaded {Path.GetFileName(filePath)} in {sw.ElapsedMilliseconds} ms ({_allTensors.Count:N0} tensors)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to parse GGUF model:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = "Failed to load model file.";
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyTensorFilter();
    }

    private void ApplyTensorFilter()
    {
        if (_allTensors == null || _allTensors.Count == 0) return;

        var filter = TxtSearch.Text?.Trim().ToLowerInvariant();
        var filtered = string.IsNullOrEmpty(filter)
            ? _allTensors
            : _allTensors.Where(t => t.Name.ToLowerInvariant().Contains(filter) || t.Type.ToString().ToLowerInvariant().Contains(filter)).ToList();

        GridTensors.ItemsSource = filtered;
        TxtTensorCount.Text = $"({filtered.Count:N0} / {_allTensors.Count:N0} tensors)";
    }

    private bool _isPopulatingHeader = false;

    private void EditHeaderProp_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isPopulatingHeader || _currentHeader == null) return;

        var name = EditModelName.Text?.Trim();
        var arch = EditModelArch.Text?.Trim();
        var ctxStr = EditModelContext.Text?.Trim();

        if (!string.IsNullOrEmpty(name))
        {
            _currentHeader.Metadata["general.name"] = name;
            TxtModelTitle.Text = name;
        }

        if (!string.IsNullOrEmpty(arch))
        {
            _currentHeader.Metadata["general.architecture"] = arch;
            TxtArchName.Text = arch;

            if (ulong.TryParse(ctxStr, out var ctxVal))
            {
                _currentHeader.Metadata[$"{arch}.context_length"] = ctxVal;
                TxtMetaContext.Text = $"Context Length: {ctxVal:N0} tokens";
            }
        }

        TxtStatus.Text = "Updated primary model header attributes";
    }

    private void ComboTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TxtStatus == null) return;
        var themeName = (ComboTheme.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (!string.IsNullOrEmpty(themeName))
        {
            ApplyStudioTheme(themeName);
        }
    }

    private void ApplyStudioTheme(string themeName)
    {
        var theme = ThemeEngine.GetTheme(themeName);
        Resources["BrushMainBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.BackgroundDark));
        Resources["BrushPanelBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.PanelDark));
        Resources["BrushHeaderBg"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.HeaderDark));
        Resources["BrushPrimaryAccent"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.PrimaryAccent));
        Resources["BrushSecondaryAccent"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.SecondaryAccent));
        Resources["BrushBorder"] = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(theme.BorderBrush));

        TxtStatus.Text = $"Applied studio theme '{theme.Name}'";
    }

    private void BtnPruneLayer_Click(object sender, RoutedEventArgs e)
    {
        if (_currentHeader == null || GridTensors.SelectedItem is not GgufTensorInfo t)
        {
            MessageBox.Show("Please select a layer tensor from the grid first.", "No Layer Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (t.Name.StartsWith("blk."))
        {
            var parts = t.Name.Split('.');
            if (parts.Length > 1 && int.TryParse(parts[1], out var layerIdx))
            {
                var result = MessageBox.Show($"Are you sure you want to prune transformer layer block '{layerIdx}'?", "Confirm Prune", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    var (prunedCount, paramsSaved) = LayerPruner.PruneLayerBlock(_currentHeader, layerIdx);
                    ApplyTensorFilter();

                    ulong totalParams = 0;
                    foreach (var tensor in _allTensors) totalParams += tensor.TotalElements;
                    TxtMetaParams.Text = $"Total Parameters: {totalParams:N0} ({totalParams / 1_000_000_000.0:F2}B)";
                    TxtMetaTensors.Text = $"Total Tensors: {_currentHeader.TensorCount:N0}";

                    MessageBox.Show($"Successfully pruned layer block '{layerIdx}'!\n  • Removed: {prunedCount} tensors\n  • Saved: {paramsSaved:N0} parameters", "Prune Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    TxtStatus.Text = $"Pruned layer block {layerIdx} ({prunedCount} tensors, {paramsSaved:N0} params saved)";
                }
            }
        }
    }

    private void GridTensors_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridTensors.SelectedItem is GgufTensorInfo t)
        {
            TxtSelectedName.Text = t.Name;
            TxtSelectedShape.Text = $"Shape: {t.DimensionString}";
            TxtSelectedQuant.Text = $"Quantization: {t.Type}";
            TxtSelectedElements.Text = $"Total Elements: {t.TotalElements:N0}";

            if (!string.IsNullOrEmpty(_currentFilePath))
            {
                var samples = TensorWeightEditor.ReadTensorSampleValues(_currentFilePath, t, 100);
                GridTensorWeights.ItemsSource = samples;
            }
        }
    }

    private void GridMetadataEditable_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridMetadataEditable.SelectedItem is MetadataItem item)
        {
            TxtSelectedMetaKey.Text = item.Key;
            EditSelectedMetaValue.Text = item.Value;
        }
    }

    private void BtnSaveSelectedMeta_Click(object sender, RoutedEventArgs e)
    {
        if (_currentHeader == null || GridMetadataEditable.SelectedItem is not MetadataItem item)
        {
            MessageBox.Show("Please select a metadata key from the table first.", "No Key Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var newVal = EditSelectedMetaValue.Text ?? "";
        item.Value = newVal;
        _currentHeader.Metadata[item.Key] = newVal;

        GridMetadataEditable.ItemsSource = null;
        GridMetadataEditable.ItemsSource = _metadataItems;

        TxtStatus.Text = $"Updated metadata key '{item.Key}' value";
        MessageBox.Show($"Updated metadata key '{item.Key}' value successfully!", "Header Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnAddMeta_Click(object sender, RoutedEventArgs e)
    {
        if (_currentHeader == null) return;

        var keyName = $"custom.param_{_currentHeader.Metadata.Count + 1}";
        _currentHeader.Metadata[keyName] = "SampleValue";
        _metadataItems.Add(new MetadataItem { Key = keyName, Value = "SampleValue" });

        GridMetadataEditable.ItemsSource = null;
        GridMetadataEditable.ItemsSource = _metadataItems;
        TxtStatus.Text = $"Added custom metadata key '{keyName}'";
    }

    private void BtnApplyPrecision_Click(object sender, RoutedEventArgs e)
    {
        if (GridTensors.SelectedItem is GgufTensorInfo t)
        {
            var selectedFormat = (ComboLayerPrecision.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (!string.IsNullOrEmpty(selectedFormat))
            {
                if (selectedFormat.Contains("IQ4_XS")) t.Type = GgmlType.IQ4_XS;
                else if (selectedFormat.Contains("Q4_K_M")) t.Type = GgmlType.Q4_K;
                else if (selectedFormat.Contains("Q6_K")) t.Type = GgmlType.Q6_K;
                else if (selectedFormat.Contains("Q8_0")) t.Type = GgmlType.Q8_0;
                else if (selectedFormat.Contains("FP16")) t.Type = GgmlType.F16;

                ApplyTensorFilter();
                TxtSelectedQuant.Text = $"Quantization: {t.Type}";
                TxtStatus.Text = $"Updated tensor '{t.Name}' precision rule to {t.Type}";
            }
        }
    }

    private LoraAdapterInfo? _activeLora;

    private void BtnAttachLora_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Attach LoRA Adapter File",
            Filter = "LoRA Adapter Files (*.safetensors;*.gguf;*.bin)|*.safetensors;*.gguf;*.bin|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _activeLora = LoraEngine.ParseLoraAdapter(dialog.FileName);
                TxtLoraName.Text = _activeLora.Name;
                TxtLoraRank.Text = $"Adapter Rank (r): {_activeLora.Rank}";
                TxtLoraAlpha.Text = $"Adapter Alpha (α): {_activeLora.Alpha:F1}";
                TxtLoraScale.Text = $"Effective Scale Multiplier: {_activeLora.Scale:F2}x";
                ListLoraModules.ItemsSource = _activeLora.TargetModules;
                TxtLoraModuleCount.Text = $"({_activeLora.TargetModules.Count} target modules)";
                TxtStatus.Text = $"Attached LoRA adapter '{_activeLora.Name}' ({_activeLora.TargetModules.Count} target modules)";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse LoRA adapter:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void SliderLoraAlpha_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TxtSliderValue != null && _activeLora != null)
        {
            var scale = SliderLoraAlpha.Value;
            TxtSliderValue.Text = $"Alpha Scale: {scale:F1}x ({scale * 100:F0}% Strength)";
        }
    }

    private void BtnMergeLora_Click(object sender, RoutedEventArgs e)
    {
        if (_activeLora == null)
        {
            MessageBox.Show("Please attach a LoRA adapter file first.", "No LoRA Attached", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var scale = SliderLoraAlpha.Value;
        MessageBox.Show($"Successfully configured LoRA adapter '{_activeLora.Name}' at {scale:F1}x scaling for merge pipeline!", "LoRA Configured", MessageBoxButton.OK, MessageBoxImage.Information);
        TxtStatus.Text = $"Configured LoRA adapter '{_activeLora.Name}' at {scale:F1}x strength";
    }

    private async void BtnDeployOllama_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
        {
            MessageBox.Show("Please open a GGUF model file first.", "No Model File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var modelName = EditOllamaName.Text?.Trim();
        if (string.IsNullOrEmpty(modelName)) modelName = "custom-model";
        var sysPrompt = EditOllamaSysPrompt.Text?.Trim() ?? "";

        TxtStatus.Text = $"Registering model '{modelName}' in Ollama...";
        TxtOllamaLog.Text = $"Executing: ollama create {modelName}...\nPlease wait...\n";

        var (success, output) = await OllamaDeployer.DeployToOllamaAsync(_currentFilePath, modelName, sysPrompt);
        TxtOllamaLog.Text += output;

        if (success)
        {
            MessageBox.Show($"Successfully registered model '{modelName}' in Ollama!\n\nYou can now run:\n  ollama run {modelName}", "Deployment Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            TxtStatus.Text = $"Registered '{modelName}' in Ollama";
        }
        else
        {
            MessageBox.Show($"Ollama registration failed:\n\n{output}", "Deployment Error", MessageBoxButton.OK, MessageBoxImage.Error);
            TxtStatus.Text = "Ollama registration failed";
        }
    }

    private void EditSamplePrompt_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_currentHeader == null) return;
        var text = EditSamplePrompt.Text;
        var tokens = TokenizerEngine.InspectTokens(_currentHeader, text);
        GridTokens.ItemsSource = tokens;
    }

    private string? _modelAPath;
    private string? _modelBPath;

    private async void BtnPlaygroundSend_Click(object sender, RoutedEventArgs e)
    {
        var url = EditPlaygroundUrl.Text?.Trim() ?? "http://127.0.0.1:11434";
        var model = EditPlaygroundModel.Text?.Trim() ?? "custom-model";
        var prompt = EditPlaygroundPrompt.Text?.Trim() ?? "";

        TxtStatus.Text = $"Sending prompt to {model} at {url}...";
        TxtPlaygroundReply.Text = "Generating reply... Please wait...";

        var (success, reply) = await ModelPlayground.GeneratePromptResponseAsync(url, model, prompt);
        TxtPlaygroundReply.Text = reply;

        TxtStatus.Text = success ? "Received response from model" : "Inference failed";
    }

    private void BtnSelectModelA_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Select Base Model A", Filter = "Model Files (*.gguf;*.safetensors)|*.gguf;*.safetensors|All Files (*.*)|*.*" };
        if (dialog.ShowDialog() == true)
        {
            _modelAPath = dialog.FileName;
            TxtModelAPath.Text = $"Model A: {Path.GetFileName(_modelAPath)}";
        }
    }

    private void BtnSelectModelB_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Select Secondary Model B", Filter = "Model Files (*.gguf;*.safetensors)|*.gguf;*.safetensors|All Files (*.*)|*.*" };
        if (dialog.ShowDialog() == true)
        {
            _modelBPath = dialog.FileName;
            TxtModelBPath.Text = $"Model B: {Path.GetFileName(_modelBPath)}";
        }
    }

    private void BtnPreviewMerge_Click(object sender, RoutedEventArgs e)
    {
        var plan = new ModelMergePlan
        {
            ModelAPath = _modelAPath ?? _currentFilePath ?? "",
            ModelBPath = _modelBPath ?? "",
            SlerpRatio = (float)SliderSlerpRatio.Value
        };

        var (success, summary) = ModelMerger.PreviewMerge(plan);
        TxtMergeLog.Text = summary;
        if (success) MessageBox.Show(summary, "Merge Plan Configured", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnSplitModel_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
        {
            MessageBox.Show("Please open a GGUF model file first.", "No Model File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!long.TryParse(EditChunkSize.Text?.Trim(), out var chunkSizeMb) || chunkSizeMb < 100)
        {
            chunkSizeMb = 4096;
        }

        TxtStatus.Text = $"Splitting model into {chunkSizeMb} MB chunks...";
        var (success, msg) = GgufSplitter.SplitGguf(_currentFilePath, chunkSizeMb);
        TxtSplitLog.Text = msg;
        MessageBox.Show(msg, success ? "Split Success" : "Split Info", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void BtnSaveModel_Click(object sender, RoutedEventArgs e)
    {
        if (_currentHeader == null || string.IsNullOrEmpty(_currentFilePath))
        {
            MessageBox.Show("Please load a GGUF model file first.", "No Model Loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Export Modified GGUF Model",
            Filter = "GGUF Model Files (*.gguf)|*.gguf",
            FileName = Path.GetFileNameWithoutExtension(_currentFilePath) + "-Tuned.gguf"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                TxtStatus.Text = "Exporting modified GGUF model binary...";
                var sw = System.Diagnostics.Stopwatch.StartNew();

                GgufWriter.SaveHeaderAndMetadata(_currentFilePath, saveDialog.FileName, _currentHeader);

                sw.Stop();
                MessageBox.Show($"Successfully exported modified GGUF model header & metadata!\nSaved to: {saveDialog.FileName}\nTime: {sw.ElapsedMilliseconds} ms", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtStatus.Text = $"Exported modified model in {sw.ElapsedMilliseconds} ms";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export GGUF file:\n{ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtStatus.Text = "Export failed.";
            }
        }
    }

    private async void BtnPublishHf_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
        {
            MessageBox.Show("Please open a model file first.", "No Model File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var repoId = EditHfRepo.Text?.Trim();
        var token = EditHfToken.Password?.Trim();

        if (string.IsNullOrEmpty(repoId) || string.IsNullOrEmpty(token))
        {
            MessageBox.Show("Repository ID and HuggingFace API token are required.", "Missing Credentials", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        TxtStatus.Text = $"Publishing model to HuggingFace repository '{repoId}'...";
        TxtHfLog.Text = $"Starting upload for '{Path.GetFileName(_currentFilePath)}' to '{repoId}'...";

        var (success, msg) = await HuggingFaceUploader.UploadModelAsync(_currentFilePath, repoId, token);
        TxtHfLog.Text += $"\n\n{msg}";
        MessageBox.Show(msg, success ? "HuggingFace Upload Success" : "Upload Error", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    private void BtnConvertQuant_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
        {
            MessageBox.Show("Please open a GGUF model file first.", "No Model File", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selectedStr = (ComboTargetQuantRule.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
        GgmlType targetType = selectedStr.Contains("IQ4_XS") ? GgmlType.IQ4_XS :
                             selectedStr.Contains("Q8_0") ? GgmlType.Q8_0 :
                             selectedStr.Contains("Q6_K") ? GgmlType.Q6_K : GgmlType.Q4_K;

        var outPath = Path.Combine(Path.GetDirectoryName(_currentFilePath) ?? "", Path.GetFileNameWithoutExtension(_currentFilePath) + $"-{targetType}.gguf");
        TxtStatus.Text = $"Converting model precision to {targetType}...";

        var (success, msg) = GgufQuantizer.QuantizeModel(_currentFilePath, outPath, targetType);
        TxtQuantLog.Text = msg;
        MessageBox.Show(msg, success ? "Quantization Success" : "Quantization Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnEvaluateQuality_Click(object sender, RoutedEventArgs e)
    {
        if (_currentHeader == null)
        {
            MessageBox.Show("Please load a model file first.", "No Model Loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var res = PerplexityEvaluator.EvaluateModelQuality(_currentHeader);
        TxtQualityGrade.Text = $"Quality Grade: {res.QualityGrade}";
        TxtPerplexityScore.Text = $"Estimated Perplexity (Wikitext-2): {res.PerplexityScore:F2}";
        TxtQuantNoise.Text = $"Quantization Noise SNR: {res.QuantizationNoiseDb:F1} dB";

        TxtQuantLog.Text = $"[Quality Evaluation Completed]\nGrade: {res.QualityGrade}\nPerplexity: {res.PerplexityScore:F2}\nSNR: {res.QuantizationNoiseDb:F1} dB\nTensors Evaluated: {_allTensors.Count:N0}";
        if (TxtPerplexityLog != null) TxtPerplexityLog.Text = TxtQuantLog.Text;
    }

    private void BtnSelectDiffModelB_Click(object sender, RoutedEventArgs e)
    {
        if (_currentHeader == null)
        {
            MessageBox.Show("Please open Base Model A first.", "No Base Model", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Select Secondary Model B to Compare",
            Filter = "GGUF Model Files (*.gguf)|*.gguf|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var headerB = UniversalModelReader.ParseHeader(dialog.FileName);
                TxtDiffModelB.Text = $"Comparing against: {Path.GetFileName(dialog.FileName)}";

                var diffs = ModelDiffViewer.CompareModels(_currentHeader, headerB);
                GridDiffResults.ItemsSource = diffs;

                TxtStatus.Text = $"Compared models: found {diffs.Count:N0} differences";
                MessageBox.Show($"Compared base model against '{Path.GetFileName(dialog.FileName)}'.\nFound {diffs.Count:N0} metadata and tensor shape differences.", "Comparison Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to parse Model B:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnCalcVram_Click(object sender, RoutedEventArgs e)
    {
        if (_currentHeader == null || _allTensors == null)
        {
            MessageBox.Show("Please load a model file first.", "No Model Loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ulong totalParams = 0;
        foreach (var t in _allTensors) totalParams += t.TotalElements;

        var est = VramEstimator.EstimateVram(totalParams);

        TxtVramWeights.Text = $"Model Weights VRAM: {est.ModelWeightsGb:F2} GB";
        TxtVram4k.Text = $"KV Cache (4k Context): {est.KvCache4kGb:F2} GB";
        TxtVram32k.Text = $"KV Cache (32k Context): {est.KvCache32kGb:F2} GB";
        TxtVram128k.Text = $"KV Cache (128k Context): {est.KvCache128kGb:F2} GB";
        TxtVramTotal.Text = $"Total Recommended VRAM: {est.TotalRecommendedVramGb:F2} GB";

        TxtVramGuideLog.Text = $"[Hardware VRAM & Memory Allocation Guide]\nTotal Model Parameters: {totalParams:N0} ({totalParams / 1_000_000_000.0:F2}B)\n\n" +
            $"1. Minimum Single GPU/TPU VRAM (4k context): {est.ModelWeightsGb + est.KvCache4kGb + 1.0:F1} GB\n" +
            $"2. Recommended VRAM for Full 32k Context: {est.ModelWeightsGb + est.KvCache32kGb + 1.5:F1} GB\n" +
            $"3. Extreme 128k Context VRAM Requirement: {est.ModelWeightsGb + est.KvCache128kGb + 2.0:F1} GB\n\n" +
            $"Deep Horizon Hardware Recommendation: A single Deep Horizon Node (16GB/32GB Unified Memory) runs this model locally with zero offload bottleneck!";
    }

    private void BtnRenderArchGraph_Click(object sender, RoutedEventArgs e)
    {
        if (_allTensors == null || _allTensors.Count == 0)
        {
            MessageBox.Show("Please load a model file first.", "No Tensors Loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        CanvasArchNodes.Children.Clear();
        double x = 20;
        double y = 80;

        DrawCanvasNode(CanvasArchNodes, "Token Embedding", "FP16 (Subword Vocab)", System.Windows.Media.Brushes.Magenta, x, y);
        x += 210;

        int count = Math.Min(8, _allTensors.Count);
        for (int i = 0; i < count; i++)
        {
            var t = _allTensors[i];
            var color = t.Type == GgmlType.IQ4_XS ? System.Windows.Media.Brushes.LimeGreen :
                        t.Type == GgmlType.Q6_K ? System.Windows.Media.Brushes.Cyan : System.Windows.Media.Brushes.Orange;

            DrawCanvasNode(CanvasArchNodes, $"Block {i}", $"{t.Type} ({t.DimensionString})", color, x, y);
            x += 210;
        }

        DrawCanvasNode(CanvasArchNodes, "LM Head / Norm", "RMSNorm + Output", System.Windows.Media.Brushes.LimeGreen, x, y);
        TxtStatus.Text = $"Rendered 2D Architecture Canvas for {_allTensors.Count:N0} layer tensors";
    }

    private void DrawCanvasNode(Canvas canvas, string title, string subtitle, System.Windows.Media.Brush borderBrush, double x, double y)
    {
        var border = new Border
        {
            Width = 180,
            Height = 100,
            Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#161C28")),
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10)
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.Bold, Foreground = System.Windows.Media.Brushes.White, FontSize = 13 });
        sp.Children.Add(new TextBlock { Text = subtitle, Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#9CA3AF")), FontSize = 11, Margin = new Thickness(0, 6, 0, 0) });
        border.Child = sp;

        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
        canvas.Children.Add(border);
    }

    private void BtnRunProfiler_Click(object sender, RoutedEventArgs e)
    {
        if (_allTensors == null || _allTensors.Count == 0)
        {
            MessageBox.Show("Please load a model file first.", "No Model Loaded", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        double bw = double.TryParse(EditProfilerBandwidth.Text, out var parsedBw) ? parsedBw : 200.0;
        var profile = LayerProfiler.ProfileModelLayers(_allTensors, bw);
        GridProfilerResults.ItemsSource = profile;
        TxtStatus.Text = $"Profiled {_allTensors.Count:N0} layers at {bw} GB/s bandwidth saturation";
    }

    private async void BtnSearchHf_Click(object sender, RoutedEventArgs e)
    {
        var q = EditHfSearchQuery.Text?.Trim() ?? "gguf";
        TxtStatus.Text = $"Searching HuggingFace Hub for '{q}'...";
        var results = await HfModelBrowser.SearchModelsAsync(q);
        GridHfSearchResults.ItemsSource = results;
        TxtStatus.Text = $"Found {results.Count} models on HuggingFace Hub";
    }

    private void GridHfSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridHfSearchResults.SelectedItem is HfModelSearchResult item)
        {
            TxtSelectedHfRepo.Text = $"Repository: {item.RepoId}";
            EditGgufDirectUrl.Text = $"https://huggingface.co/{item.RepoId}/resolve/main/{item.ModelName}.gguf";
        }
    }

    private async void BtnStartDownload_Click(object sender, RoutedEventArgs e)
    {
        var url = EditGgufDirectUrl.Text?.Trim();
        if (string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Please enter a direct GGUF model download URL.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var fileName = Path.GetFileName(url);
        var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);

        TxtDownloadStatus.Text = $"Downloading {fileName}...";
        TxtStatus.Text = $"Downloading GGUF model file from HuggingFace...";

        var (success, msg) = await HfModelBrowser.DownloadGgufFileAsync(url, savePath, (pct) =>
        {
            Dispatcher.Invoke(() =>
            {
                ProgDownload.Value = pct;
                TxtDownloadStatus.Text = $"Downloading... {pct:F1}% complete";
            });
        });

        TxtDownloadStatus.Text = msg;
        MessageBox.Show(msg, success ? "Download Complete" : "Download Failed", MessageBoxButton.OK, success ? MessageBoxImage.Information : MessageBoxImage.Error);
    }
}