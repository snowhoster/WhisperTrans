# Whisper.NET 引擎整合說明

## 概述

WhisperNetEngine 現在已整合真實的 Whisper.NET 函式庫，可以進行實際的語音轉錄。

## 使用的套件

### Whisper.net
- **版本**: 1.7.3
- **用途**: Whisper 語音識別引擎的 .NET 封裝
- **專案**: https://github.com/sandrohanea/whisper.net
- **授權**: MIT License

### Whisper.net.Runtime
- **版本**: 1.7.3
- **用途**: Whisper 原生函式庫運行時
- **包含**: 跨平台的 Whisper.cpp 二進制檔案

## 功能特性

### 1. 完整的語音轉錄
```csharp
// 自動處理音訊資料並轉錄為文字
var result = await engine.TranscribeAsync(audioSegment);
Console.WriteLine(result.Text);
```

### 2. 語言支援
支援多種語言的轉錄：
- 中文 (zh)
- 英文 (en)
- 日文 (ja)
- 韓文 (ko)
- 以及 Whisper 支援的其他 90+ 種語言

### 3. 自動語言偵測
```csharp
var config = new WhisperConfig
{
    Language = null  // null 表示自動偵測
};
```

### 4. 翻譯功能
```csharp
var config = new WhisperConfig
{
    Translate = true  // 將語音翻譯為英文
};
```

### 5. VAD 整合
整合語音活動檢測（Voice Activity Detection）：
- 自動過濾靜音片段
- 減少不必要的處理
- 提升整體效能

### 6. 多執行緒處理
```csharp
var config = new WhisperConfig
{
    ThreadCount = Environment.ProcessorCount  // 使用所有 CPU 核心
};
```

## 實作細節

### 初始化流程

```csharp
public async Task InitializeAsync(WhisperConfig config)
{
    // 1. 載入模型檔案
    _whisperFactory = WhisperFactory.FromPath(config.ModelPath);
    
    // 2. 建立處理器
    var builder = _whisperFactory.CreateBuilder()
        .WithThreads(config.ThreadCount);
    
    // 3. 設定語言（可選）
    if (!string.IsNullOrEmpty(config.Language))
    {
        builder = builder.WithLanguage(config.Language);
    }
    
    // 4. 設定翻譯模式（可選）
    if (config.Translate)
    {
        builder = builder.WithTranslate();
    }
    
    // 5. 設定提示文字（提高準確度）
    builder = builder.WithPrompt("繁體中文語音轉錄。");
    
    // 6. 建立最終處理器
    _processor = builder.Build();
}
```

### 轉錄流程

```csharp
public async Task<TranscriptionResult> TranscribeAsync(AudioSegment segment)
{
    // 1. 檢查 VAD（如果啟用）
    if (_config?.EnableVAD && !segment.ContainsSpeech)
    {
        return EmptyResult();  // 跳過靜音片段
    }
    
    // 2. 執行轉錄
    await foreach (var result in _processor.ProcessAsync(audioData))
    {
        transcriptionText += result.Text;
        confidence = Math.Max(confidence, result.Probability);
        detectedLanguage = result.Language;
    }
    
    // 3. 返回結果
    return new TranscriptionResult
    {
        Text = transcriptionText.Trim(),
        Language = detectedLanguage,
        Confidence = confidence,
        ProcessingTimeMs = elapsedTime,
        Timestamp = segment.StartTime,
        IsFinal = true
    };
}
```

## 配置選項

### WhisperConfig 參數說明

```csharp
var config = new WhisperConfig
{
    // 必要參數
    ModelPath = "models/ggml-base.bin",  // 模型檔案路徑
    
    // 語言設定
    Language = "zh",                      // 指定語言，null=自動偵測
    
    // 效能設定
    UseGpu = true,                        // GPU 加速（需要硬體支援）
    ThreadCount = 4,                      // CPU 執行緒數量
    
    // 音訊分段
    SegmentDuration = 2.0,                // 片段長度（秒）
    SegmentOverlap = 0.5,                 // 片段重疊（秒）
    
    // VAD 設定
    EnableVAD = true,                     // 啟用語音活動檢測
    VadThreshold = 0.02f,                 // VAD 閾值（0-1）
    MinSilenceDurationMs = 500,           // 最小靜音時間（毫秒）
    
    // 進階選項
    Translate = false                     // 翻譯為英文
};
```

## 效能優化

### 1. VAD 過濾
```csharp
// 跳過靜音片段，節省處理時間
if (_config?.EnableVAD && !segment.ContainsSpeech)
{
    return EmptyResult();
}
```

### 2. 多執行緒處理
```csharp
// 使用所有可用的 CPU 核心
ThreadCount = Environment.ProcessorCount
```

### 3. 批次處理
```csharp
// 一次處理多個音訊片段
var results = await TranscribeBatchAsync(segments);
```

### 4. 記憶體管理
```csharp
// 正確釋放資源
public void Dispose()
{
    _processor?.Dispose();
    _whisperFactory?.Dispose();
}
```

## 錯誤處理

### 1. 初始化錯誤
```csharp
try
{
    await engine.InitializeAsync(config);
}
catch (FileNotFoundException)
{
    Console.WriteLine("找不到模型檔案");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"初始化失敗: {ex.Message}");
}
```

### 2. 轉錄錯誤
```csharp
// 引擎會自動處理轉錄錯誤
// 返回包含錯誤訊息的結果，不會中斷流程
var result = await engine.TranscribeAsync(segment);
if (result.Text.StartsWith("[轉錄錯誤"))
{
    Console.WriteLine("轉錄過程發生錯誤");
}
```

### 3. 取消處理
```csharp
var cts = new CancellationTokenSource();
try
{
    var result = await engine.TranscribeAsync(segment, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("轉錄已取消");
}
```

## 效能指標

### 不同模型的效能比較

| 模型 | 大小 | 速度 | 準確度 | 記憶體 | 建議用途 |
|------|------|------|--------|--------|----------|
| tiny | 75 MB | 最快 | 較低 | ~390 MB | 測試/原型 |
| base | 142 MB | 快 | 中等 | ~500 MB | 一般使用 ? |
| small | 466 MB | 中等 | 好 | ~1 GB | 高品質 |
| medium | 1.5 GB | 慢 | 很好 | ~2.5 GB | 專業用途 |
| large | 2.9 GB | 最慢 | 最好 | ~4.5 GB | 最高品質 |

### 即時處理效能

在一般硬體配置下（Intel i5, 16GB RAM）：

- **tiny**: 0.5x 實際時間（超即時）
- **base**: 1x 實際時間（即時）?
- **small**: 2x 實際時間
- **medium**: 4x 實際時間
- **large**: 8x 實際時間

> ?? 建議：一般即時轉錄使用 **base** 模型

## 使用範例

### 基本使用
```csharp
// 1. 建立配置
var config = new WhisperConfig
{
    ModelPath = "models/ggml-base.bin",
    Language = "zh",
    EnableVAD = true
};

// 2. 初始化引擎
var engine = new WhisperNetEngine();
await engine.InitializeAsync(config);

// 3. 轉錄音訊
var result = await engine.TranscribeAsync(audioSegment);
Console.WriteLine($"轉錄結果: {result.Text}");
Console.WriteLine($"信心度: {result.Confidence:P0}");
Console.WriteLine($"處理時間: {result.ProcessingTimeMs}ms");

// 4. 清理資源
engine.Dispose();
```

### 即時轉錄
```csharp
// 結合 RealtimeTranscriptionService 使用
var transcriptionService = new RealtimeTranscriptionService(engine, audioCapture);

transcriptionService.TranscriptionReceived += (sender, result) =>
{
    Console.WriteLine($"[{result.Timestamp:F2}s] {result.Text}");
};

await transcriptionService.StartAsync();
```

### 批次處理
```csharp
var segments = new List<AudioSegment> { segment1, segment2, segment3 };
var results = await engine.TranscribeBatchAsync(segments);

foreach (var result in results)
{
    Console.WriteLine(result.Text);
}
```

### 多語言支援
```csharp
// 自動偵測語言
var config1 = new WhisperConfig
{
    Language = null  // 自動偵測
};

// 指定語言
var config2 = new WhisperConfig
{
    Language = "en"  // 英文
};

// 翻譯為英文
var config3 = new WhisperConfig
{
    Language = "zh",
    Translate = true  // 中文轉英文
};
```

## 疑難排解

### Q: 轉錄速度太慢
**A**: 
1. 使用較小的模型（如 tiny 或 base）
2. 增加執行緒數量
3. 啟用 VAD 過濾靜音
4. 檢查是否有 GPU 加速

### Q: 轉錄結果不準確
**A**:
1. 使用較大的模型（如 small 或 medium）
2. 確保音訊品質良好
3. 指定正確的語言
4. 調整 VAD 閾值

### Q: 記憶體使用過高
**A**:
1. 使用較小的模型
2. 減少執行緒數量
3. 適當調整音訊片段大小
4. 確保正確釋放資源

### Q: 初始化失敗
**A**:
1. 確認模型檔案存在且完整
2. 檢查模型檔案格式正確（.bin）
3. 確保有足夠的磁碟空間
4. 檢查檔案權限

## 技術規格

### 支援的音訊格式
- **採樣率**: 16000 Hz（固定）
- **聲道**: 單聲道（Mono）
- **位元深度**: 16-bit PCM
- **資料格式**: Float32 array（-1.0 to 1.0）

### 系統需求
- **.NET**: 10.0 或更高
- **作業系統**: Windows 10/11, Linux, macOS
- **記憶體**: 最少 4 GB RAM（建議 8 GB+）
- **CPU**: 多核心處理器（建議 4 核心+）
- **GPU**: 可選（CUDA 支援可加速處理）

## 授權資訊

### Whisper.net
- MIT License
- 開源專案
- 免費用於商業和個人用途

### Whisper.cpp
- MIT License
- OpenAI Whisper 的 C++ 實作
- 免費用於商業和個人用途

## 相關資源

- [Whisper.net GitHub](https://github.com/sandrohanea/whisper.net)
- [Whisper.cpp GitHub](https://github.com/ggerganov/whisper.cpp)
- [OpenAI Whisper](https://github.com/openai/whisper)
- [模型下載](https://huggingface.co/ggerganov/whisper.cpp)

## 更新日誌

### v1.2.0 - Whisper.NET 整合
- ? 整合 Whisper.net 1.7.3
- ? 實作實際的語音轉錄功能
- ? 支援多語言自動偵測
- ? 支援翻譯功能
- ? 整合 VAD 過濾
- ? 完整的錯誤處理
- ? 效能優化

---

**版本**: 1.2.0  
**最後更新**: 2024
