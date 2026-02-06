using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using WhisperTrans.Core.Audio;
using WhisperTrans.Core.Engines;
using WhisperTrans.Core.Models;
using WhisperTrans.Core.Services;
using WhisperTrans.Core.Interfaces;

namespace WhisperTrans.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private IWhisperEngine? _whisperEngine;
    private NAudioCapture? _audioCapture;
    private RealtimeTranscriptionService? _transcriptionService;
    private VoiceActivityDetector? _vad;
    private LLMTranslationService? _translationService;
    private ITextToSpeechService? _ttsService;
    private bool _isRecording;
    private CancellationTokenSource? _cts;
    private ModelDownloader? _modelDownloader;
    private List<AudioDeviceInfo> _audioDevices = new();
    private int _selectedDeviceIndex = -1;
    private DispatcherTimer? _visualizerTimer;
    private bool _isTranslationEnabled;

    public MainWindow()
    {
        InitializeComponent();
        _modelDownloader = new ModelDownloader();
        _translationService = new LLMTranslationService();
        _ttsService = new WindowsTtsService();
        LoadAudioDevices();
        InitializeVisualizer();
        InitializeTranslationUI();
        InitializeTtsUI();
        UpdateUIState(false);
    }

    private void InitializeTranslationUI()
    {
        // 確保 UI 控制項已初始化
        if (EnableTranslationCheckBox == null)
            return;
            
        // 預設停用即時翻譯功能，但保持設定控制項可用
        EnableTranslationCheckBox.IsChecked = false;
        
        // 不要停用翻譯設定控制項，讓使用者可以設定
        // UpdateTranslationUIState(false);  // ← 移除這行
        
        // 嘗試載入上次儲存的設定
        LoadTranslationSettings();
    }

    private void UpdateTranslationUIState(bool enabled)
    {
        // 安全檢查，確保所有控制項都已初始化
        if (LLMProviderComboBox == null || ApiUrlTextBox == null || 
            ApiKeyPasswordBox == null || TranslationTargetComboBox == null || 
            ModelNameTextBox == null || TestConnectionButton == null)
            return;
        
        // 只在使用者勾選「啟用即時翻譯」時才控制這些控制項的啟用狀態
        // 但我們始終保持它們可用，只是在啟用翻譯時會驗證設定
        // LLMProviderComboBox.IsEnabled = enabled;
        // ApiUrlTextBox.IsEnabled = enabled;
        // ApiKeyPasswordBox.IsEnabled = enabled;
        // ApiKeyTextBox.IsEnabled = enabled;
        // ShowApiKeyCheckBox.IsEnabled = enabled;
        // TranslationTargetComboBox.IsEnabled = enabled;
        // ModelNameTextBox.IsEnabled = enabled;
        // TestConnectionButton.IsEnabled = enabled;
        // SaveSettingsButton.IsEnabled = enabled;
        // LoadSettingsButton.IsEnabled = enabled;
        
        // 這些控制項始終可用，讓使用者可以隨時設定
    }

    private void InitializeVisualizer()
    {
        _visualizerTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _visualizerTimer.Tick += (s, e) =>
        {
            if (!_isRecording)
            {
                // 錄音停止時，逐漸降低視覺化效果
                AudioVisualizer.AudioLevel *= 0.9f;
            }
        };
        _visualizerTimer.Start();
    }

    private void LoadAudioDevices()
    {
        try
        {
            _audioDevices = NAudioCapture.GetAvailableDevices();
            AudioDeviceComboBox.ItemsSource = _audioDevices;
            
            if (_audioDevices.Count > 0)
            {
                AudioDeviceComboBox.SelectedIndex = 0;
                _selectedDeviceIndex = 0;
                CurrentDeviceText.Text = _audioDevices[0].DisplayName;
            }
            else
            {
                CurrentDeviceText.Text = "未偵測到音訊裝置";
                MessageBox.Show("未偵測到任何音訊輸入裝置，請確認麥克風已正確連接。", 
                    "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"載入音訊裝置失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshDevicesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecording)
        {
            MessageBox.Show("請先停止錄音再重新整理裝置列表", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        LoadAudioDevices();
        StatusText.Text = "音訊裝置列表已更新";
    }

    private void AudioDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AudioDeviceComboBox.SelectedItem is AudioDeviceInfo deviceInfo)
        {
            _selectedDeviceIndex = deviceInfo.DeviceIndex;
            CurrentDeviceText.Text = deviceInfo.DisplayName;
            
            if (!_isRecording)
            {
                StatusText.Text = $"已選擇音訊裝置: {deviceInfo.DisplayName}";
            }
        }
    }

    private async void InitializeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "初始化中...";
            InitializeButton.IsEnabled = false;

            // 檢查是否選擇了音訊裝置
            if (_audioDevices.Count == 0)
            {
                MessageBox.Show("未偵測到音訊輸入裝置，請連接麥克風後重新整理裝置列表。", 
                    "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                InitializeButton.IsEnabled = true;
                return;
            }

            // 取得引擎類型
            var engineType = (WhisperEngineComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "remote"
                ? WhisperEngineType.Remote
                : WhisperEngineType.Local;

            // 建立配置
            var config = new WhisperConfig
            {
                EngineType = engineType,
                Language = GetSelectedLanguage(),
                UseGpu = UseGpuCheckBox.IsChecked ?? false,
                SegmentDuration = 2.0,
                SegmentOverlap = 0.5,
                EnableVAD = EnableVadCheckBox.IsChecked ?? true,
                VadThreshold = 0.02f,
                MinSilenceDurationMs = 500
            };

            // 根據引擎類型設定對應參數
            if (engineType == WhisperEngineType.Remote)
            {
                // 遠端 API 配置
                var apiUrl = RemoteApiUrlTextBox.Text;
                if (string.IsNullOrWhiteSpace(apiUrl))
                {
                    MessageBox.Show("請輸入遠端 API URL", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    InitializeButton.IsEnabled = true;
                    return;
                }

                config.RemoteApiUrl = apiUrl;
                
                // 初始化遠端引擎
                _whisperEngine = new RemoteWhisperEngine();
                await _whisperEngine.InitializeAsync(config);

                StatusText.Text = $"遠端 Whisper ASR 已初始化 - {apiUrl}";
            }
            else
            {
                // 本地模型配置
                var modelPath = ModelPathTextBox.Text;
                if (string.IsNullOrWhiteSpace(modelPath))
                {
                    MessageBox.Show("請輸入模型檔案路徑", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                    InitializeButton.IsEnabled = true;
                    return;
                }

                config.ModelPath = modelPath;

                // 自動下載模型（如果不存在）
                if (!File.Exists(modelPath))
                {
                    var result = MessageBox.Show(
                        $"找不到模型檔案：{modelPath}\n\n是否要自動下載模型？\n\n模型：{Path.GetFileName(modelPath)}\n預估大小：{ModelDownloader.GetEstimatedModelSize(Path.GetFileName(modelPath))} MB",
                        "模型不存在",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            var progress = new Progress<DownloadProgress>(p =>
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    StatusText.Text = p.FormattedMessage;
                                });
                            });

                            await _modelDownloader!.EnsureModelExistsAsync(modelPath, progress);
                            
                            MessageBox.Show("模型下載完成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"模型下載失敗: {ex.Message}\n\n請手動下載模型檔案:\n1. 訪問: https://huggingface.co/ggerganov/whisper.cpp\n2. 下載 {Path.GetFileName(modelPath)}\n3. 放置於: {Path.GetDirectoryName(modelPath)}", 
                                "下載錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                            InitializeButton.IsEnabled = true;
                            StatusText.Text = "模型下載失敗";
                            return;
                        }
                    }
                    else
                    {
                        InitializeButton.IsEnabled = true;
                        StatusText.Text = "請選擇有效的模型檔案";
                        return;
                    }
                }

                // 初始化本地引擎
                _whisperEngine = new WhisperNetEngine();
                await _whisperEngine.InitializeAsync(config);

                StatusText.Text = $"本地 Whisper.net 已初始化 - {Path.GetFileName(modelPath)}";
            }

            // 初始化 VAD
            _vad = new VoiceActivityDetector(
                threshold: config.VadThreshold,
                minSilenceDurationMs: config.MinSilenceDurationMs
            );

            // 初始化音訊捕捉
            _audioCapture = new NAudioCapture(
                sampleRate: 16000,
                channels: 1,
                segmentDuration: config.SegmentDuration,
                vad: _vad,
                deviceNumber: _selectedDeviceIndex
            );

            // 訂閱音訊層級事件
            _audioCapture.AudioLevelChanged += OnAudioLevelChanged;

            // 初始化轉錄服務
            _transcriptionService = new RealtimeTranscriptionService(_whisperEngine, _audioCapture);

            _transcriptionService.TranscriptionReceived += OnTranscriptionReceived;
            _transcriptionService.PartialTranscriptionReceived += OnPartialTranscriptionReceived;
            _transcriptionService.ErrorOccurred += OnErrorOccurred;

            StatusText.Text += " - 準備開始錄音";
            AudioStatusText.Text = "就緒";
            UpdateUIState(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"初始化失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = "初始化失敗";
            InitializeButton.IsEnabled = true;
        }
    }

    private void OnAudioLevelChanged(object? sender, AudioLevelEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            AudioVisualizer.AudioLevel = e.Level;
            AudioVisualizer.PeakLevel = e.PeakLevel;
            AudioVisualizer.IsSpeechDetected = e.IsSpeechDetected;

            // 更新語音指示器
            if (e.IsSpeechDetected)
            {
                SpeechIndicator.Fill = new SolidColorBrush(Colors.Green);
                AudioStatusText.Text = "偵測到語音";
            }
            else if (e.Level > 0.01f)
            {
                SpeechIndicator.Fill = new SolidColorBrush(Colors.Orange);
                AudioStatusText.Text = "偵測到音訊";
            }
            else
            {
                SpeechIndicator.Fill = new SolidColorBrush(Colors.Gray);
                AudioStatusText.Text = "靜音";
            }
        });
    }

    private async void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_transcriptionService == null)
            return;

        try
        {
            if (!_isRecording)
            {
                _cts = new CancellationTokenSource();
                await _transcriptionService.StartAsync(_cts.Token);
                _isRecording = true;
                StartStopButton.Content = "⏸️ 停止錄音";
                StatusText.Text = "🎤 錄音中...";
                AudioStatusText.Text = "錄音中";
            }
            else
            {
                await _transcriptionService.StopAsync();
                _cts?.Cancel();
                _isRecording = false;
                StartStopButton.Content = "🎤 開始錄音";
                StatusText.Text = "錄音已停止";
                AudioStatusText.Text = "已停止";
                
                // 重置視覺化
                AudioVisualizer.AudioLevel = 0;
                SpeechIndicator.Fill = new SolidColorBrush(Colors.Gray);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"錄音操作失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        TranscriptionTextBox.Clear();
        _transcriptionService?.ClearHistory();
        StatusText.Text = "轉錄歷史已清除";
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"轉錄_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".txt",
                Filter = "文字檔案 (.txt)|*.txt|所有檔案 (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, TranscriptionTextBox.Text);
                MessageBox.Show("匯出成功!", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"匯出失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BrowseModelButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Whisper 模型 (*.bin)|*.bin|所有檔案 (*.*)|*.*",
            Title = "選擇 Whisper 模型檔案"
        };

        if (dialog.ShowDialog() == true)
        {
            ModelPathTextBox.Text = dialog.FileName;
        }
    }

    private async void DownloadModelButton_Click(object sender, RoutedEventArgs e)
    {
        var modelSelectionWindow = new Window
        {
            Title = "下載 Whisper 模型",
            Width = 500,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize
        };

        var panel = new StackPanel { Margin = new Thickness(20) };
        
        panel.Children.Add(new TextBlock 
        { 
            Text = "選擇要下載的模型：", 
            FontSize = 14, 
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        var listBox = new ListBox { Height = 200, Margin = new Thickness(0, 0, 0, 10) };
        var models = new[]
        {
            new { Name = "ggml-tiny.bin", Size = "75 MB", Description = "最小模型，速度最快，精確度較低" },
            new { Name = "ggml-base.bin", Size = "142 MB", Description = "基礎模型，推薦一般使用" },
            new { Name = "ggml-small.bin", Size = "466 MB", Description = "小型模型，較好的精確度" },
            new { Name = "ggml-medium.bin", Size = "1464 MB", Description = "中型模型，高精確度" },
            new { Name = "ggml-large-v3.bin", Size = "2950 MB", Description = "大型模型，最高精確度" }
        };

        foreach (var model in models)
        {
            listBox.Items.Add($"{model.Name} ({model.Size}) - {model.Description}");
        }
        listBox.SelectedIndex = 1; // 預設選擇 base
        
        panel.Children.Add(listBox);

        var outputPathPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        outputPathPanel.Children.Add(new TextBlock { Text = "儲存位置：", VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
        var pathTextBox = new TextBox { Width = 250, Text = "models", VerticalContentAlignment = VerticalAlignment.Center };
        outputPathPanel.Children.Add(pathTextBox);
        panel.Children.Add(outputPathPanel);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var downloadBtn = new Button { Content = "下載", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0) };
        var cancelBtn = new Button { Content = "取消", Width = 80, Height = 30 };
        
        downloadBtn.Click += async (s, args) =>
        {
            if (listBox.SelectedIndex < 0)
            {
                MessageBox.Show("請選擇一個模型", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedModel = models[listBox.SelectedIndex];
            var outputDir = pathTextBox.Text;
            var outputPath = Path.Combine(outputDir, selectedModel.Name);

            modelSelectionWindow.Close();

            try
            {
                DownloadModelButton.IsEnabled = false;
                InitializeButton.IsEnabled = false;

                var progress = new Progress<DownloadProgress>(p =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = p.FormattedMessage;
                    });
                });

                await _modelDownloader!.DownloadModelAsync(selectedModel.Name, outputPath, progress);
                
                ModelPathTextBox.Text = outputPath;
                MessageBox.Show($"模型下載完成！\n\n檔案位置：{outputPath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"下載失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DownloadModelButton.IsEnabled = true;
                InitializeButton.IsEnabled = true;
                StatusText.Text = "請先初始化系統";
            }
        };

        cancelBtn.Click += (s, args) => modelSelectionWindow.Close();

        buttonPanel.Children.Add(downloadBtn);
        buttonPanel.Children.Add(cancelBtn);
        panel.Children.Add(buttonPanel);

        modelSelectionWindow.Content = panel;
        modelSelectionWindow.ShowDialog();
    }

    private void OnTranscriptionReceived(object? sender, TranscriptionResult result)
    {
        Dispatcher.Invoke(async () =>
        {
            var timestamp = TimeSpan.FromSeconds(result.Timestamp).ToString(@"hh\:mm\:ss");
            TranscriptionTextBox.AppendText($"[{timestamp}] {result.Text}\n");
            TranscriptionTextBox.ScrollToEnd();
            
            StatusText.Text = $"最後轉錄: {DateTime.Now:HH:mm:ss} | 處理時間: {result.ProcessingTimeMs}ms | 信心度: {result.Confidence:P0}";

            // 如果啟用翻譯，自動翻譯新的轉錄文字
            if (_isTranslationEnabled && !string.IsNullOrWhiteSpace(result.Text))
            {
                await TranslateTextAsync(result.Text, timestamp);
            }
        });
    }

    private async Task TranslateTextAsync(string text, string timestamp)
    {
        try
        {
            if (_translationService == null)
                return;

            StatusText.Text = "翻譯中...";
            
            var translation = await _translationService.TranslateAsync(text);
            
            if (!string.IsNullOrWhiteSpace(translation))
            {
                TranslationTextBox.AppendText($"[{timestamp}] {translation}\n");
                TranslationTextBox.ScrollToEnd();
                StatusText.Text = $"翻譯完成 | {DateTime.Now:HH:mm:ss}";

                // 如果啟用自動朗讀，朗讀翻譯結果
                if (EnableTtsCheckBox?.IsChecked == true && _ttsService != null)
                {
                    try
                    {
                        // 設定翻譯目標語言的語音
                        var targetLanguage = (TranslationTargetComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "zh-TW";
                        SetTtsVoiceForLanguage(targetLanguage);

                        // 非同步朗讀（不等待完成）
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _ttsService.SpeakAsync(translation);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"自動朗讀失敗: {ex.Message}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"啟動自動朗讀失敗: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            TranslationTextBox.AppendText($"[{timestamp}] [翻譯錯誤: {ex.Message}]\n");
            StatusText.Text = $"翻譯失敗: {ex.Message}";
        }
    }

    private void EnableTranslationCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        _isTranslationEnabled = EnableTranslationCheckBox.IsChecked ?? false;
        UpdateTranslationUIState(_isTranslationEnabled);

        if (_isTranslationEnabled)
        {
            ConfigureTranslationService();
        }
    }

    private void ConfigureTranslationService()
    {
        try
        {
            var apiUrl = ApiUrlTextBox.Text;
            var apiKey = GetApiKey();
            var modelName = ModelNameTextBox.Text;
            var targetLanguage = (TranslationTargetComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "zh-TW";
            var provider = (LLMProviderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "openai";

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                MessageBox.Show("請輸入 API URL", "設定錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                EnableTranslationCheckBox.IsChecked = false;
                return;
            }

            // 本地模型（vLLM 和 Ollama）不強制要求 API Key
            var isLocalModel = provider == "vllm" || provider == "ollama";
            
            if (string.IsNullOrWhiteSpace(apiKey) && !isLocalModel)
            {
                MessageBox.Show("請輸入 API Key", "設定錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                EnableTranslationCheckBox.IsChecked = false;
                return;
            }

            _translationService?.Configure(apiUrl, apiKey, modelName, targetLanguage);
            StatusText.Text = isLocalModel ? "本地翻譯服務已啟用" : "翻譯服務已啟用";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"配置翻譯服務失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            EnableTranslationCheckBox.IsChecked = false;
        }
    }

    private string GetApiKey()
    {
        // 根據顯示狀態從不同的控制項取得 API Key
        if (ShowApiKeyCheckBox?.IsChecked == true)
        {
            return ApiKeyTextBox?.Text ?? string.Empty;
        }
        else
        {
            return ApiKeyPasswordBox?.Password ?? string.Empty;
        }
    }

    private void SetApiKey(string apiKey)
    {
        // 同時設定兩個控制項的值
        if (ApiKeyPasswordBox != null)
            ApiKeyPasswordBox.Password = apiKey;
        if (ApiKeyTextBox != null)
            ApiKeyTextBox.Text = apiKey;
    }

    private void ShowApiKeyCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        if (ShowApiKeyCheckBox == null || ApiKeyPasswordBox == null || ApiKeyTextBox == null)
            return;

        var isChecked = ShowApiKeyCheckBox.IsChecked ?? false;
        
        if (isChecked)
        {
            // 顯示 API Key - 切換到 TextBox
            ApiKeyTextBox.Text = ApiKeyPasswordBox.Password;
            ApiKeyPasswordBox.Visibility = Visibility.Collapsed;
            ApiKeyTextBox.Visibility = Visibility.Visible;
        }
        else
        {
            // 隱藏 API Key - 切換到 PasswordBox
            ApiKeyPasswordBox.Password = ApiKeyTextBox.Text;
            ApiKeyPasswordBox.Visibility = Visibility.Visible;
            ApiKeyTextBox.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = new
            {
                Provider = (LLMProviderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
                ApiUrl = ApiUrlTextBox.Text,
                ApiKey = GetApiKey(),
                ModelName = ModelNameTextBox.Text,
                TargetLanguage = (TranslationTargetComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true 
            });

            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "translation_settings.json");
            File.WriteAllText(settingsPath, json);

            MessageBox.Show($"設定已儲存至:\n{settingsPath}", "儲存成功", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "翻譯設定已儲存";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"儲存設定失敗: {ex.Message}", "錯誤", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        LoadTranslationSettings(true);
    }

    private void LoadTranslationSettings(bool showMessage = false)
    {
        try
        {
            var settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "translation_settings.json");
            
            if (!File.Exists(settingsPath))
            {
                if (showMessage)
                {
                    MessageBox.Show("找不到儲存的設定檔案", "提示", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return;
            }

            var json = File.ReadAllText(settingsPath);
            var settings = System.Text.Json.JsonSerializer.Deserialize<TranslationSettings>(json);

            if (settings == null)
                return;

            // 載入設定到 UI
            if (!string.IsNullOrEmpty(settings.Provider))
            {
                foreach (ComboBoxItem item in LLMProviderComboBox.Items)
                {
                    if (item.Tag?.ToString() == settings.Provider)
                    {
                        LLMProviderComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(settings.ApiUrl))
                ApiUrlTextBox.Text = settings.ApiUrl;

            if (!string.IsNullOrEmpty(settings.ApiKey))
                SetApiKey(settings.ApiKey);

            if (!string.IsNullOrEmpty(settings.ModelName))
                ModelNameTextBox.Text = settings.ModelName;

            if (!string.IsNullOrEmpty(settings.TargetLanguage))
            {
                foreach (ComboBoxItem item in TranslationTargetComboBox.Items)
                {
                    if (item.Tag?.ToString() == settings.TargetLanguage)
                    {
                        TranslationTargetComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            if (showMessage)
            {
                MessageBox.Show("設定已載入", "載入成功", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            
            StatusText.Text = "翻譯設定已載入";
        }
        catch (Exception ex)
        {
            if (showMessage)
            {
                MessageBox.Show($"載入設定失敗: {ex.Message}", "錯誤", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // 設定類別
    private class TranslationSettings
    {
        public string? Provider { get; set; }
        public string? ApiUrl { get; set; }
        public string? ApiKey { get; set; }
        public string? ModelName { get; set; }
        public string? TargetLanguage { get; set; }
    }

    private void LLMProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LLMProviderComboBox.SelectedItem is not ComboBoxItem item)
            return;

        // 確保 UI 控制項已初始化
        if (ApiUrlTextBox == null || ModelNameTextBox == null)
            return;

        var provider = item.Tag?.ToString();
        
        // 根據提供商更新預設 URL 和模型
        switch (provider)
        {
            case "openai":
                ApiUrlTextBox.Text = "https://api.openai.com/v1/chat/completions";
                ModelNameTextBox.Text = "gpt-3.5-turbo";
                ShowApiKeyRequired(true);
                break;
            case "azure":
                ApiUrlTextBox.Text = "https://YOUR-RESOURCE.openai.azure.com/openai/deployments/YOUR-DEPLOYMENT/chat/completions?api-version=2024-02-15-preview";
                ModelNameTextBox.Text = "gpt-35-turbo";
                ShowApiKeyRequired(true);
                break;
            case "anthropic":
                ApiUrlTextBox.Text = "https://api.anthropic.com/v1/messages";
                ModelNameTextBox.Text = "claude-3-sonnet-20240229";
                ShowApiKeyRequired(true);
                break;
            case "google":
                ApiUrlTextBox.Text = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
                ModelNameTextBox.Text = "gemini-pro";
                ShowApiKeyRequired(true);
                break;
            case "ollama":
                ApiUrlTextBox.Text = "http://localhost:11434/api/chat";
                ModelNameTextBox.Text = "llama2";
                ShowApiKeyRequired(false);
                break;
            case "vllm":
                // vLLM 支援兩種 API 格式
                // 預設使用 Chat Completions（更推薦）
                ApiUrlTextBox.Text = "http://localhost:8000/v1/chat/completions";
                ModelNameTextBox.Text = "模型名稱（依您部署的模型）";
                ShowApiKeyRequired(false);
                
                // 顯示提示訊息
                if (MessageBox.Show(
                    "vLLM 支援兩種 API 格式：\n\n" +
                    "1. Chat Completions (推薦)\n" +
                    "   URL: /v1/chat/completions\n" +
                    "   適合對話式模型\n\n" +
                    "2. Completions (舊版)\n" +
                    "   URL: /v1/completions\n" +
                    "   適合基礎模型\n\n" +
                    "是否使用舊版 Completions API？\n" +
                    "（預設為 Chat Completions）",
                    "選擇 API 格式",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    ApiUrlTextBox.Text = "http://localhost:8000/v1/completions";
                }
                break;
            case "custom":
                ApiUrlTextBox.Text = "";
                ModelNameTextBox.Text = "";
                ShowApiKeyRequired(true);
                break;
        }
    }

    private void ShowApiKeyRequired(bool required)
    {
        // 更新 UI 提示使用者是否需要 API Key
        if (ApiKeyPasswordBox == null)
            return;

        if (required)
        {
            ApiKeyPasswordBox.ToolTip = "您的 API 金鑰（必填）";
        }
        else
        {
            ApiKeyPasswordBox.ToolTip = "API 金鑰（選填，本地模型通常不需要）";
            // 清空 API Key 欄位（如果不需要的話）
            if (string.IsNullOrWhiteSpace(ApiKeyPasswordBox.Password))
            {
                ApiKeyPasswordBox.Password = "";
            }
        }
    }

    private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TestConnectionButton.IsEnabled = false;
            StatusText.Text = "測試連線中...";

            // 先驗證基本設定
            var apiUrl = ApiUrlTextBox.Text;
            var apiKey = GetApiKey();
            var modelName = ModelNameTextBox.Text;
            var targetLanguage = (TranslationTargetComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "zh-TW";
            var provider = (LLMProviderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "openai";

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                MessageBox.Show("請先輸入 API URL", "設定錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusText.Text = "請設定 API URL";
                return;
            }

            // 本地模型不強制要求 API Key
            var isLocalModel = provider == "vllm" || provider == "ollama";
            
            if (string.IsNullOrWhiteSpace(apiKey) && !isLocalModel)
            {
                MessageBox.Show("請先輸入 API Key", "設定錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusText.Text = "請設定 API Key";
                return;
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                MessageBox.Show("請先輸入模型名稱", "設定錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusText.Text = "請設定模型名稱";
                return;
            }

            // 配置服務
            if (_translationService == null)
            {
                MessageBox.Show("翻譯服務未初始化", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _translationService.Configure(apiUrl, apiKey, modelName, targetLanguage);

            // 測試連線
            System.Diagnostics.Debug.WriteLine($"測試連線 - URL: {apiUrl}");
            System.Diagnostics.Debug.WriteLine($"測試連線 - 模型: {modelName}");
            System.Diagnostics.Debug.WriteLine($"測試連線 - 提供商: {provider}");

            var success = await _translationService.TestConnectionAsync();

            if (success)
            {
                var message = isLocalModel 
                    ? $"✓ 連線成功！\n\n本地模型 API 可以正常使用。\n\n提供商: {provider}\n模型: {modelName}"
                    : $"✓ 連線成功！\n\nAPI 可以正常使用。\n\n提供商: {provider}\n模型: {modelName}";

                MessageBox.Show(message, "測試成功", MessageBoxButton.OK, MessageBoxImage.Information);
                StatusText.Text = isLocalModel ? "本地模型連線測試成功" : "API 連線測試成功";
            }
            else
            {
                var troubleshooting = isLocalModel
                    ? "請檢查：\n1. vLLM/Ollama 伺服器是否已啟動\n2. API URL 是否正確\n3. 模型名稱是否正確\n4. 網路連線是否正常"
                    : "請檢查：\n1. API URL 是否正確\n2. API Key 是否有效\n3. 模型名稱是否正確\n4. 網路連線是否正常\n5. 是否超過配額限制";

                MessageBox.Show($"✗ 連線失敗\n\n{troubleshooting}", 
                    "測試失敗", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "連線測試失敗";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"測試連線異常: {ex}");
            
            var errorMessage = $"測試連線時發生錯誤:\n\n{ex.Message}";
            
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\n詳細錯誤: {ex.InnerException.Message}";
            }

            MessageBox.Show(errorMessage, "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = $"連線測試錯誤: {ex.Message}";
        }
        finally
        {
            TestConnectionButton.IsEnabled = true;
        }
    }

    private void ClearTranslationButton_Click(object sender, RoutedEventArgs e)
    {
        TranslationTextBox.Clear();
        StatusText.Text = "翻譯歷史已清除";
    }

    private void ExportTranslationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"翻譯_{DateTime.Now:yyyyMMdd_HHmmss}",
                DefaultExt = ".txt",
                Filter = "文字檔案 (.txt)|*.txt|所有檔案 (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, TranslationTextBox.Text);
                MessageBox.Show("翻譯匯出成功!", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"匯出失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnPartialTranscriptionReceived(object? sender, string text)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = $"[部分] {text}";
        });
    }

    private void OnErrorOccurred(object? sender, Exception ex)
    {
        Dispatcher.Invoke(() =>
        {
            MessageBox.Show($"轉錄錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = $"錯誤: {ex.Message}";
        });
    }

    private void UpdateUIState(bool initialized)
    {
        StartStopButton.IsEnabled = initialized;
        ClearButton.IsEnabled = initialized;
        ExportButton.IsEnabled = initialized;
        
        ModelPathTextBox.IsEnabled = !initialized;
        BrowseModelButton.IsEnabled = !initialized;
        DownloadModelButton.IsEnabled = !initialized;
        AudioDeviceComboBox.IsEnabled = !initialized;
        RefreshDevicesButton.IsEnabled = !initialized;
        LanguageComboBox.IsEnabled = !initialized;
        UseGpuCheckBox.IsEnabled = !initialized;
        EnableVadCheckBox.IsEnabled = !initialized;
    }

    private string? GetSelectedLanguage()
    {
        if (LanguageComboBox.SelectedItem is ComboBoxItem item)
        {
            var tag = item.Tag?.ToString();
            return tag == "auto" ? null : tag;
        }
        return "zh"; // 預設繁體中文
    }

    private void InitializeTtsUI()
    {
        // 確保 UI 控制項已初始化
        if (EnableTtsCheckBox == null)
            return;
            
        // 預設停用自動朗讀
        EnableTtsCheckBox.IsChecked = false;
        
        // 設定預設語音（根據翻譯目標語言）
        SetTtsVoiceForLanguage("zh-TW");
    }

    private void SetTtsVoiceForLanguage(string languageCode)
    {
        if (_ttsService == null)
            return;

        // 根據語言選擇合適的語音
        var voiceName = languageCode switch
        {
            "zh-TW" => "Microsoft Hanhan Desktop",      // 繁體中文
            "zh-CN" => "Microsoft Huihui Desktop",      // 簡體中文
            "en" => "Microsoft Zira Desktop",            // 英文
            "ja" => "Microsoft Haruka Desktop",          // 日文
            "ko" => "Microsoft Heami Desktop",           // 韓文
            _ => null
        };

        if (!string.IsNullOrEmpty(voiceName))
        {
            try
            {
                _ttsService.SetVoice(voiceName);
            }
            catch
            {
                // 如果找不到指定語音，使用預設語音
                System.Diagnostics.Debug.WriteLine($"無法設定語音: {voiceName}");
            }
        }

        // 設定語速和音量
        _ttsService.SetRate(1.0);  // 正常速度
        _ttsService.SetVolume(0.8); // 80% 音量
    }

    private async void SpeakTranscriptionButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = TranscriptionTextBox.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("沒有可朗讀的轉錄文字", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_ttsService == null)
            {
                MessageBox.Show("TTS 服務未初始化", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 設定原文語言的語音
            var sourceLanguage = GetSelectedLanguage() ?? "zh";
            SetTtsVoiceForLanguage(sourceLanguage);

            SpeakTranscriptionButton.IsEnabled = false;
            StatusText.Text = "正在朗讀轉錄文字...";

            await _ttsService.SpeakAsync(text);

            StatusText.Text = "朗讀完成";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"朗讀失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = $"朗讀錯誤: {ex.Message}";
        }
        finally
        {
            SpeakTranscriptionButton.IsEnabled = true;
        }
    }

    private async void SpeakTranslationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var text = TranslationTextBox.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("沒有可朗讀的翻譯文字", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_ttsService == null)
            {
                MessageBox.Show("TTS 服務未初始化", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 設定翻譯目標語言的語音
            var targetLanguage = (TranslationTargetComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "zh-TW";
            SetTtsVoiceForLanguage(targetLanguage);

            SpeakTranslationButton.IsEnabled = false;
            StatusText.Text = "正在朗讀翻譯文字...";

            await _ttsService.SpeakAsync(text);

            StatusText.Text = "朗讀完成";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"朗讀失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = $"朗讀錯誤: {ex.Message}";
        }
        finally
        {
            SpeakTranslationButton.IsEnabled = true;
        }
    }

    private void StopSpeakingButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _ttsService?.Stop();
            StatusText.Text = "已停止朗讀";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"停止朗讀失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        _visualizerTimer?.Stop();
        
        if (_isRecording)
        {
            _transcriptionService?.StopAsync().GetAwaiter().GetResult();
        }

        if (_audioCapture != null)
        {
            _audioCapture.AudioLevelChanged -= OnAudioLevelChanged;
        }

        _transcriptionService?.Dispose();
        _audioCapture?.Dispose();
        _whisperEngine?.Dispose();
        _translationService?.Dispose();
        _ttsService?.Dispose();
        _cts?.Dispose();
    }

    private void WhisperEngineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WhisperEngineComboBox?.SelectedItem is not ComboBoxItem item)
            return;

        var engineType = item.Tag?.ToString();
        var isRemote = engineType == "remote";

        // 切換顯示對應的配置控制項
        if (ModelPathLabel != null && RemoteApiUrlLabel != null &&
            ModelPathTextBox != null && RemoteApiUrlTextBox != null &&
            BrowseModelButton != null && DownloadModelButton != null &&
            TestRemoteApiButton != null)
        {
            if (isRemote)
            {
                // 顯示遠端 API 配置
                ModelPathLabel.Visibility = Visibility.Collapsed;
                ModelPathTextBox.Visibility = Visibility.Collapsed;
                BrowseModelButton.Visibility = Visibility.Collapsed;
                DownloadModelButton.Visibility = Visibility.Collapsed;

                RemoteApiUrlLabel.Visibility = Visibility.Visible;
                RemoteApiUrlTextBox.Visibility = Visibility.Visible;
                TestRemoteApiButton.Visibility = Visibility.Visible;

                // GPU 選項對遠端 API 無效
                if (UseGpuCheckBox != null)
                {
                    UseGpuCheckBox.IsEnabled = false;
                    UseGpuCheckBox.ToolTip = "遠端 API 模式下此選項無效";
                }

                StatusText.Text = "請設定遠端 Whisper ASR API URL";
            }
            else
            {
                // 顯示本地模型配置
                ModelPathLabel.Visibility = Visibility.Visible;
                ModelPathTextBox.Visibility = Visibility.Visible;
                BrowseModelButton.Visibility = Visibility.Visible;
                DownloadModelButton.Visibility = Visibility.Visible;

                RemoteApiUrlLabel.Visibility = Visibility.Collapsed;
                RemoteApiUrlTextBox.Visibility = Visibility.Collapsed;
                TestRemoteApiButton.Visibility = Visibility.Collapsed;

                // 恢復 GPU 選項
                if (UseGpuCheckBox != null)
                {
                    UseGpuCheckBox.IsEnabled = true;
                    UseGpuCheckBox.ToolTip = null;
                }

                StatusText.Text = "請先初始化系統";
            }
        }
    }

    private async void TestRemoteApiButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            TestRemoteApiButton.IsEnabled = false;
            StatusText.Text = "測試遠端 API 連線中...";

            var apiUrl = RemoteApiUrlTextBox.Text;
            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                MessageBox.Show("請先輸入遠端 API URL", "設定錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusText.Text = "請設定遠端 API URL";
                return;
            }

            // 建立測試音訊（1 秒的靜音）
            var sampleRate = 16000;
            var testAudio = new float[sampleRate];
            
            // 建立測試引擎
            var testConfig = new WhisperConfig
            {
                EngineType = WhisperEngineType.Remote,
                RemoteApiUrl = apiUrl,
                Language = GetSelectedLanguage()
            };

            using var testEngine = new RemoteWhisperEngine();
            await testEngine.InitializeAsync(testConfig);

            // 發送測試請求
            var testSegment = new AudioSegment
            {
                Samples = testAudio,
                SampleRate = sampleRate,
                StartTime = 0,
                Duration = 1.0
            };

            var result = await testEngine.TranscribeAsync(testSegment);

            MessageBox.Show(
                $"✓ 連線成功！\n\n" +
                $"遠端 API 可以正常使用。\n\n" +
                $"API URL: {apiUrl}\n" +
                $"處理時間: {result.ProcessingTimeMs:F0}ms",
                "測試成功",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            
            StatusText.Text = "遠端 API 連線測試成功";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"遠端 API 測試失敗: {ex}");

            var errorMessage = $"連線失敗:\n\n{ex.Message}";
            
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\n詳細錯誤: {ex.InnerException.Message}";
            }

            errorMessage += "\n\n請檢查：\n" +
                "1. API URL 是否正確\n" +
                "2. 遠端服務是否已啟動\n" +
                "3. 網路連線是否正常\n" +
                "4. 防火牆設定";

            MessageBox.Show(errorMessage, "測試失敗", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusText.Text = $"連線測試失敗: {ex.Message}";
        }
        finally
        {
            TestRemoteApiButton.IsEnabled = true;
        }
    }
}
