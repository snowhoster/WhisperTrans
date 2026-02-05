# WhisperTrans - 即時語音轉文字系統

基於 OpenAI Whisper 的 .NET 10 即時語音轉文字解決方案，採用滑動視窗機制實現低延遲轉錄。

## 🌟 特點

- ✅ **即時轉錄**: 採用滑動視窗（Sliding Window）機制，實現近乎即時的語音轉文字
- ✅ **語音活動檢測**: 內建 VAD (Voice Activity Detection) 自動過濾靜音片段
- ✅ **多語言支援**: 支援中文、英文、日文、韓文等多種語言，可自動偵測
- ✅ **GPU 加速**: 可選擇使用 GPU 加速推理速度
- ✅ **上下文保留**: 保留最近的轉錄歷史，確保語意連貫
- ✅ **雙介面**: 提供控制台和 WPF GUI 兩種使用方式

## 📦 專案結構

```
WhisperTrans/
├── WhisperTrans.sln                    # 解決方案檔
├── src/
│   ├── WhisperTrans.Core/              # 核心函式庫
│   │   ├── Models/                     # 資料模型
│   │   ├── Interfaces/                 # 介面定義
│   │   ├── Services/                   # 核心服務
│   │   ├── Engines/                    # Whisper 引擎實現
│   │   └── Audio/                      # 音訊擷取
│   ├── WhisperTrans.Console/           # 控制台應用
│   └── WhisperTrans.Desktop/           # WPF 桌面應用
└── README.md
```

## 🚀 快速開始

### 前置需求

- .NET 10 SDK
- Windows 10/11 (用於 WPF 應用)
- 麥克風設備

### 安裝步驟

1. **克隆或下載專案**
   ```bash
   git clone <repository-url>
   cd WhisperTrans
   ```

2. **下載 Whisper 模型**
   
   前往 [Whisper.cpp 模型下載頁](https://huggingface.co/ggerganov/whisper.cpp/tree/main)
   
   推薦下載以下其中一個模型：
   - `ggml-base.bin` (約 140 MB) - 平衡速度與準確度
   - `ggml-small.bin` (約 460 MB) - 更高準確度
   - `ggml-medium.bin` (約 1.5 GB) - 專業級準確度
   
   將下載的模型檔案放置於 `models/` 目錄下。

3. **還原 NuGet 套件**
   ```bash
   dotnet restore
   ```

4. **建置專案**
   ```bash
   dotnet build
   ```

### 使用控制台應用

```bash
cd src/WhisperTrans.Console
dotnet run
```

操作指令：
- `Enter` - 開始/停止錄音
- `C` - 清除轉錄歷史
- `S` - 顯示完整轉錄
- `Q` - 退出程式

### 使用 WPF 桌面應用

```bash
cd src/WhisperTrans.Desktop
dotnet run
```

GUI 操作：
1. 選擇模型檔案路徑
2. 選擇語言（或自動偵測）
3. 設定 GPU 加速和 VAD 選項
4. 點擊「初始化」
5. 點擊「🎤 開始錄音」開始即時轉錄
6. 可隨時匯出轉錄結果為文字檔

## 🔧 整合實際 Whisper 引擎

目前專案提供的是架構框架，需要整合實際的 Whisper 引擎才能運作。

### 推薦方案 1: Whisper.NET

[Whisper.NET](https://github.com/sandrohanea/whisper.net) 是 Whisper.cpp 的 .NET 綁定，效能優異。

**安裝套件:**
```bash
cd src/WhisperTrans.Core
dotnet add package Whisper.net
dotnet add package Whisper.net.Runtime
```

**在 `WhisperNetEngine.cs` 中整合:**

```csharp
using Whisper.net;

private WhisperProcessor? _processor;

public async Task InitializeAsync(WhisperConfig config, CancellationToken cancellationToken = default)
{
    // ... 驗證代碼 ...
    
    using var whisperFactory = WhisperFactory.FromPath(config.ModelPath);
    _processor = whisperFactory.CreateBuilder()
        .WithLanguage(config.Language ?? "auto")
        .WithThreads(config.ThreadCount)
        .Build();
    
    _isInitialized = true;
}

public async Task<TranscriptionResult> TranscribeAsync(AudioSegment segment, CancellationToken cancellationToken = default)
{
    var stopwatch = Stopwatch.StartNew();
    
    await foreach (var result in _processor.ProcessAsync(segment.Samples, cancellationToken))
    {
        return new TranscriptionResult
        {
            Text = result.Text,
            Language = result.Language,
            Confidence = 0.95f, // Whisper.net 可能不提供信心度
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
            Timestamp = segment.StartTime,
            IsFinal = true
        };
    }
    
    return new TranscriptionResult();
}
```

### 推薦方案 2: 使用 Whisper.cpp 直接整合

通過 P/Invoke 直接呼叫 Whisper.cpp 的 C API。

參考 [Whisper.cpp](https://github.com/ggerganov/whisper.cpp) 專案。

## 📊 技術架構

### 滑動視窗機制

```
時間軸: |-------|-------|-------|-------|
片段 1: [=======]
片段 2:     [=======]
片段 3:         [=======]
片段 4:             [=======]
```

每個片段：
- 持續時間：2 秒（可配置）
- 重疊時間：0.5 秒（可配置）
- 確保上下文連貫性

### VAD (語音活動檢測)

- 使用 RMS (Root Mean Square) 能量檢測
- 自動過濾靜音片段，減少不必要的推理
- 可根據環境調整靈敏度

### 非同步處理流程

```
麥克風 → 音訊緩衝 → VAD 檢測 → 轉錄佇列 → Whisper 引擎 → 結果輸出
```

## ⚙️ 配置參數

### WhisperConfig 參數說明

| 參數 | 類型 | 預設值 | 說明 |
|------|------|--------|------|
| ModelPath | string | - | Whisper 模型檔案路徑 |
| Language | string? | null | 語言代碼（null = 自動偵測） |
| UseGpu | bool | true | 是否使用 GPU 加速 |
| ThreadCount | int | CPU 核心數 | 推理執行緒數量 |
| SegmentDuration | double | 2.0 | 音訊片段長度（秒） |
| SegmentOverlap | double | 0.5 | 片段重疊時間（秒） |
| EnableVAD | bool | true | 啟用語音活動檢測 |
| VadThreshold | float | 0.5 | VAD 靜音閾值 (0-1) |
| MinSilenceDurationMs | int | 500 | 最小靜音持續時間（毫秒） |

## 🎯 使用場景

- 🎤 **會議記錄**: 即時轉錄會議內容
- 📝 **語音筆記**: 快速將口述內容轉為文字
- 🌐 **即時字幕**: 為影片或直播生成即時字幕
- 🔊 **無障礙輔助**: 為聽障人士提供即時文字輔助
- 📞 **客服記錄**: 記錄客服通話內容

## 🛠️ 開發者指南

### 自訂 Whisper 引擎

實現 `IWhisperEngine` 介面：

```csharp
public class CustomWhisperEngine : IWhisperEngine
{
    public async Task InitializeAsync(WhisperConfig config, CancellationToken cancellationToken = default)
    {
        // 初始化邏輯
    }

    public async Task<TranscriptionResult> TranscribeAsync(AudioSegment segment, CancellationToken cancellationToken = default)
    {
        // 轉錄邏輯
    }

    // ... 其他方法
}
```

### 自訂音訊擷取

實現 `IAudioCapture` 介面：

```csharp
public class CustomAudioCapture : IAudioCapture
{
    public event EventHandler<AudioSegment>? AudioSegmentCaptured;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        // 開始錄音
    }

    // ... 其他方法
}
```

## 📝 授權

MIT License

## 🤝 貢獻

歡迎提交 Issue 和 Pull Request！

## 📚 參考資源

- [OpenAI Whisper](https://github.com/openai/whisper)
- [Whisper.cpp](https://github.com/ggerganov/whisper.cpp)
- [Whisper.NET](https://github.com/sandrohanea/whisper.net)
- [NAudio](https://github.com/naudio/NAudio)
- [Faster-Whisper](https://github.com/guillaumekln/faster-whisper)

## ❓ 常見問題

### Q: 為什麼轉錄結果是「模擬轉錄結果」？
A: 需要整合實際的 Whisper 引擎，請參考「整合實際 Whisper 引擎」章節。

### Q: 如何提高轉錄準確度？
A: 
1. 使用更大的模型（如 medium 或 large）
2. 調整 VAD 靈敏度參數
3. 確保麥克風音質良好
4. 減少環境噪音

### Q: 支援哪些語言？
A: Whisper 支援 99 種語言，包括中文、英文、日文、韓文等。完整列表請參考 [Whisper 官方文檔](https://github.com/openai/whisper#available-models-and-languages)。

### Q: 可以用於錄音檔轉錄嗎？
A: 可以！你可以擴展 `IAudioCapture` 介面來讀取音訊檔案，而不是從麥克風錄音。

---

**開發者**: WhisperTrans Team  
**版本**: 1.0.0  
**更新日期**: 2026-02-04
