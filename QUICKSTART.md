# WhisperTrans 快速開始指南

## 🎯 5 分鐘快速體驗

### 步驟 1: 下載 Whisper 模型

1. 訪問 [Whisper.cpp 模型下載頁](https://huggingface.co/ggerganov/whisper.cpp/tree/main)
2. 下載 `ggml-base.bin` (約 140 MB)
3. 將檔案放置於 `models/` 目錄下

```
WhisperTrans/
└── models/
    └── ggml-base.bin  ← 下載的模型放這裡
```

### 步驟 2: 整合 Whisper.NET (必要步驟)

當前專案只包含架構，需要整合實際的 Whisper 引擎。

#### 安裝 Whisper.NET 套件

```bash
cd src/WhisperTrans.Core
dotnet add package Whisper.net
dotnet add package Whisper.net.Runtime
```

#### 修改 WhisperNetEngine.cs

打開 `src/WhisperTrans.Core/Engines/WhisperNetEngine.cs`，將以下代碼：

**找到這段（第 6 行左右）:**
```csharp
using WhisperTrans.Core.Interfaces;
using WhisperTrans.Core.Models;

namespace WhisperTrans.Core.Engines;
```

**改為:**
```csharp
using WhisperTrans.Core.Interfaces;
using WhisperTrans.Core.Models;
using Whisper.net;
using Whisper.net.Ggml;

namespace WhisperTrans.Core.Engines;
```

**找到 InitializeAsync 方法（約第 25 行），將:**
```csharp
// TODO: 初始化實際的 Whisper 模型
// 範例（需要安裝 Whisper.net NuGet 套件）:
// using var whisperFactory = WhisperFactory.FromPath(config.ModelPath);
// _processor = whisperFactory.CreateBuilder()
//     .WithLanguage(config.Language)
//     .WithThreads(config.ThreadCount)
//     .Build();

await Task.CompletedTask;
_isInitialized = true;
```

**改為:**
```csharp
using var whisperFactory = WhisperFactory.FromPath(config.ModelPath);
_processor = whisperFactory.CreateBuilder()
    .WithLanguage(config.Language ?? "auto")
    .WithThreads(config.ThreadCount)
    .Build();

await Task.CompletedTask;
_isInitialized = true;
```

**找到 TranscribeAsync 方法（約第 50 行），將:**
```csharp
// TODO: 實際的轉錄邏輯
// 範例:
// var result = await _processor.ProcessAsync(segment.Samples, cancellationToken);

// 暫時返回模擬結果
await Task.Delay(50, cancellationToken); // 模擬處理時間

var result = new TranscriptionResult
{
    Text = "[模擬轉錄結果 - 請整合實際的 Whisper 引擎]",
    Language = _config?.Language ?? "zh",
    Confidence = 0.95f,
    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
    Timestamp = segment.StartTime,
    IsFinal = true
};

return result;
```

**改為:**
```csharp
var resultText = string.Empty;
await foreach (var segmentData in _processor.ProcessAsync(segment.Samples, cancellationToken))
{
    resultText += segmentData.Text;
}

var result = new TranscriptionResult
{
    Text = resultText.Trim(),
    Language = _config?.Language ?? "auto",
    Confidence = 0.95f,
    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
    Timestamp = segment.StartTime,
    IsFinal = true
};

return result;
```

**在類別中添加欄位（第 15 行左右），找到:**
```csharp
private WhisperConfig? _config;
private bool _isInitialized;
private bool _disposed;
```

**改為:**
```csharp
private WhisperConfig? _config;
private bool _isInitialized;
private bool _disposed;
private WhisperProcessor? _processor;
```

**找到 Dispose 方法（約第 90 行），將:**
```csharp
// TODO: 清理 Whisper 資源
// _processor?.Dispose();
```

**改為:**
```csharp
_processor?.Dispose();
```

### 步驟 3: 重新建置專案

```bash
dotnet build
```

### 步驟 4: 執行應用程式

#### 方式 A: 控制台應用

```bash
cd src/WhisperTrans.Console
dotnet run
```

操作：
1. 輸入模型路徑或按 Enter 使用預設路徑
2. 按 Enter 開始錄音
3. 開始說話，即時查看轉錄結果
4. 再按 Enter 停止錄音

#### 方式 B: WPF 桌面應用

```bash
cd src/WhisperTrans.Desktop
dotnet run
```

操作：
1. 點擊「瀏覽...」選擇模型檔案
2. 選擇語言（或保持「自動偵測」）
3. 點擊「初始化」
4. 點擊「🎤 開始錄音」
5. 開始說話，轉錄結果會即時顯示
6. 點擊「匯出文字」保存結果

## 📝 完整範例代碼

如果你想用程式碼方式使用：

```csharp
using WhisperTrans.Core.Audio;
using WhisperTrans.Core.Engines;
using WhisperTrans.Core.Models;
using WhisperTrans.Core.Services;

// 1. 配置
var config = new WhisperConfig
{
    ModelPath = "models/ggml-base.bin",
    Language = "zh",
    UseGpu = true,
    EnableVAD = true
};

// 2. 初始化引擎
using var engine = new WhisperNetEngine();
await engine.InitializeAsync(config);

// 3. 創建音訊擷取
var vad = new VoiceActivityDetector(threshold: 0.02f);
using var audioCapture = new NAudioCapture(vad: vad);

// 4. 創建轉錄服務
using var service = new RealtimeTranscriptionService(engine, audioCapture);

// 5. 註冊事件
service.TranscriptionReceived += (sender, result) =>
{
    Console.WriteLine($"轉錄結果: {result.Text}");
};

// 6. 開始轉錄
await service.StartAsync();

// 等待用戶輸入後停止
Console.ReadLine();
await service.StopAsync();

// 7. 取得完整結果
var fullText = service.GetFullTranscription();
Console.WriteLine($"\n完整轉錄:\n{fullText}");
```

## ⚡ 效能優化建議

### GPU 加速

確保啟用 GPU 加速以獲得最佳效能：

```csharp
var config = new WhisperConfig
{
    UseGpu = true,  // ← 啟用此選項
    // ...
};
```

### 模型選擇

根據需求選擇合適的模型：

| 使用場景 | 推薦模型 | 原因 |
|---------|---------|------|
| 即時會議記錄 | base / small | 平衡速度與準確度 |
| 語音筆記 | base | 速度快，日常使用足夠 |
| 專業轉錄 | medium / large | 準確度最高 |
| 快速測試 | tiny | 最快，適合開發測試 |

### VAD 調整

如果在安靜環境使用，降低 VAD 閾值：

```csharp
var config = new WhisperConfig
{
    VadThreshold = 0.01f,  // 預設 0.02，降低可提高靈敏度
    // ...
};
```

如果環境噪音大，提高 VAD 閾值：

```csharp
var config = new WhisperConfig
{
    VadThreshold = 0.05f,  // 提高閾值過濾噪音
    // ...
};
```

## 🔧 疑難排解

### 問題 1: 找不到模型檔案

```
錯誤: 找不到模型檔案: models/ggml-base.bin
```

**解決方案:**
1. 確認已下載模型檔案
2. 檢查檔案路徑是否正確
3. 使用絕對路徑：`C:\path\to\models\ggml-base.bin`

### 問題 2: 麥克風無法使用

```
錯誤: NAudio 初始化失敗
```

**解決方案:**
1. Windows 設定 → 隱私 → 麥克風 → 允許應用程式存取麥克風
2. 確認麥克風已連接且正常工作
3. 檢查是否有其他程式佔用麥克風

### 問題 3: 轉錄速度太慢

```
處理時間: 5000ms (超過音訊長度)
```

**解決方案:**
1. 使用更小的模型（tiny 或 base）
2. 啟用 GPU 加速
3. 減少 SegmentDuration（如改為 1.5 秒）
4. 增加 ThreadCount

### 問題 4: 轉錄結果不準確

**解決方案:**
1. 使用更大的模型（medium 或 large）
2. 調整麥克風音量
3. 減少環境噪音
4. 調整 VAD 參數
5. 指定正確的語言代碼

## 🎓 學習資源

- [OpenAI Whisper 官方文檔](https://github.com/openai/whisper)
- [Whisper.NET GitHub](https://github.com/sandrohanea/whisper.net)
- [NAudio 教學](https://github.com/naudio/NAudio)
- [專案架構說明](ARCHITECTURE.md)

## 💬 獲取幫助

遇到問題？
1. 查看 [README.md](README.md) 完整文檔
2. 查看 [ARCHITECTURE.md](ARCHITECTURE.md) 技術細節
3. 在 GitHub 提交 Issue

祝您使用愉快！ 🎉
