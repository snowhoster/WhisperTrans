using WhisperTrans.Core.Audio;
using WhisperTrans.Core.Engines;
using WhisperTrans.Core.Models;
using WhisperTrans.Core.Services;

namespace WhisperTrans.Examples;

/// <summary>
/// Whisper.NET 引擎使用範例
/// </summary>
public static class WhisperEngineExamples
{
    /// <summary>
    /// 範例 1: 基本轉錄
    /// </summary>
    public static async Task BasicTranscriptionExample()
    {
        Console.WriteLine("=== 範例 1: 基本轉錄 ===\n");

        // 建立配置
        var config = new WhisperConfig
        {
            ModelPath = "models/ggml-base.bin",
            Language = "zh",
            EnableVAD = true,
            VadThreshold = 0.02f
        };

        // 初始化引擎
        using var engine = new WhisperNetEngine();
        await engine.InitializeAsync(config);
        Console.WriteLine("? 引擎初始化完成\n");

        // 模擬音訊資料（實際使用時從麥克風或檔案讀取）
        var audioSegment = CreateMockAudioSegment();

        // 執行轉錄
        var result = await engine.TranscribeAsync(audioSegment);

        // 顯示結果
        Console.WriteLine($"轉錄文字: {result.Text}");
        Console.WriteLine($"語言: {result.Language}");
        Console.WriteLine($"信心度: {result.Confidence:P0}");
        Console.WriteLine($"處理時間: {result.ProcessingTimeMs}ms\n");
    }

    /// <summary>
    /// 範例 2: 即時轉錄
    /// </summary>
    public static async Task RealtimeTranscriptionExample()
    {
        Console.WriteLine("=== 範例 2: 即時轉錄 ===\n");

        var config = new WhisperConfig
        {
            ModelPath = "models/ggml-base.bin",
            Language = "zh",
            EnableVAD = true,
            SegmentDuration = 2.0
        };

        using var engine = new WhisperNetEngine();
        await engine.InitializeAsync(config);

        var vad = new VoiceActivityDetector(0.02f, 500);
        using var audioCapture = new NAudioCapture(
            sampleRate: 16000,
            channels: 1,
            segmentDuration: 2.0,
            vad: vad
        );

        using var service = new RealtimeTranscriptionService(engine, audioCapture);

        // 訂閱事件
        service.TranscriptionReceived += (sender, result) =>
        {
            Console.WriteLine($"[{result.Timestamp:F2}s] {result.Text}");
            Console.WriteLine($"  ??  處理時間: {result.ProcessingTimeMs}ms");
            Console.WriteLine($"  ?? 信心度: {result.Confidence:P0}\n");
        };

        service.PartialTranscriptionReceived += (sender, text) =>
        {
            Console.Write($"\r[部分] {text}");
        };

        // 開始轉錄
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        Console.WriteLine("?? 錄音中... (按任意鍵停止)");
        Console.ReadKey();

        await service.StopAsync();
        Console.WriteLine("\n? 錄音已停止");
    }

    /// <summary>
    /// 範例 3: 批次處理
    /// </summary>
    public static async Task BatchProcessingExample()
    {
        Console.WriteLine("=== 範例 3: 批次處理 ===\n");

        var config = new WhisperConfig
        {
            ModelPath = "models/ggml-base.bin",
            Language = "zh"
        };

        using var engine = new WhisperNetEngine();
        await engine.InitializeAsync(config);

        // 建立多個音訊片段
        var segments = new List<AudioSegment>
        {
            CreateMockAudioSegment(),
            CreateMockAudioSegment(),
            CreateMockAudioSegment()
        };

        Console.WriteLine($"準備處理 {segments.Count} 個音訊片段...\n");

        // 批次轉錄
        var results = await engine.TranscribeBatchAsync(segments);

        // 顯示結果
        var resultList = results.ToList();
        for (int i = 0; i < resultList.Count; i++)
        {
            var result = resultList[i];
            Console.WriteLine($"片段 {i + 1}:");
            Console.WriteLine($"  文字: {result.Text}");
            Console.WriteLine($"  處理時間: {result.ProcessingTimeMs}ms\n");
        }
    }

    /// <summary>
    /// 範例 4: 多語言轉錄
    /// </summary>
    public static async Task MultiLanguageExample()
    {
        Console.WriteLine("=== 範例 4: 多語言轉錄 ===\n");

        var languages = new[] { "zh", "en", "ja", "ko" };

        foreach (var lang in languages)
        {
            Console.WriteLine($"--- {lang.ToUpper()} ---");

            var config = new WhisperConfig
            {
                ModelPath = "models/ggml-base.bin",
                Language = lang
            };

            using var engine = new WhisperNetEngine();
            await engine.InitializeAsync(config);

            var segment = CreateMockAudioSegment();
            var result = await engine.TranscribeAsync(segment);

            Console.WriteLine($"轉錄: {result.Text}");
            Console.WriteLine($"偵測語言: {result.Language}\n");
        }
    }

    /// <summary>
    /// 範例 5: 自動語言偵測
    /// </summary>
    public static async Task AutoDetectLanguageExample()
    {
        Console.WriteLine("=== 範例 5: 自動語言偵測 ===\n");

        var config = new WhisperConfig
        {
            ModelPath = "models/ggml-base.bin",
            Language = null  // null = 自動偵測
        };

        using var engine = new WhisperNetEngine();
        await engine.InitializeAsync(config);
        Console.WriteLine("? 使用自動語言偵測模式\n");

        var segment = CreateMockAudioSegment();
        var result = await engine.TranscribeAsync(segment);

        Console.WriteLine($"轉錄文字: {result.Text}");
        Console.WriteLine($"偵測到的語言: {result.Language}");
        Console.WriteLine($"信心度: {result.Confidence:P0}\n");
    }

    /// <summary>
    /// 範例 6: 翻譯功能
    /// </summary>
    public static async Task TranslationExample()
    {
        Console.WriteLine("=== 範例 6: 翻譯功能 ===\n");

        // 配置 1: 僅轉錄中文
        var config1 = new WhisperConfig
        {
            ModelPath = "models/ggml-base.bin",
            Language = "zh",
            Translate = false
        };

        using var engine1 = new WhisperNetEngine();
        await engine1.InitializeAsync(config1);
        var result1 = await engine1.TranscribeAsync(CreateMockAudioSegment());
        Console.WriteLine($"中文轉錄: {result1.Text}\n");

        // 配置 2: 轉錄並翻譯為英文
        var config2 = new WhisperConfig
        {
            ModelPath = "models/ggml-base.bin",
            Language = "zh",
            Translate = true
        };

        using var engine2 = new WhisperNetEngine();
        await engine2.InitializeAsync(config2);
        var result2 = await engine2.TranscribeAsync(CreateMockAudioSegment());
        Console.WriteLine($"英文翻譯: {result2.Text}\n");
    }

    /// <summary>
    /// 範例 7: VAD 過濾
    /// </summary>
    public static async Task VADFilteringExample()
    {
        Console.WriteLine("=== 範例 7: VAD 過濾 ===\n");

        var config = new WhisperConfig
        {
            ModelPath = "models/ggml-base.bin",
            Language = "zh",
            EnableVAD = true,
            VadThreshold = 0.02f,
            MinSilenceDurationMs = 500
        };

        using var engine = new WhisperNetEngine();
        await engine.InitializeAsync(config);

        // 有語音的片段
        var speechSegment = CreateMockAudioSegment();
        speechSegment.ContainsSpeech = true;

        var result1 = await engine.TranscribeAsync(speechSegment);
        Console.WriteLine($"有語音: {result1.Text}");
        Console.WriteLine($"處理時間: {result1.ProcessingTimeMs}ms\n");

        // 靜音片段
        var silenceSegment = CreateMockAudioSegment();
        silenceSegment.ContainsSpeech = false;

        var result2 = await engine.TranscribeAsync(silenceSegment);
        Console.WriteLine($"靜音: '{result2.Text}' (已跳過)");
        Console.WriteLine($"處理時間: {result2.ProcessingTimeMs}ms (幾乎為 0)\n");
    }

    /// <summary>
    /// 範例 8: 效能比較
    /// </summary>
    public static async Task PerformanceComparisonExample()
    {
        Console.WriteLine("=== 範例 8: 效能比較 ===\n");

        var models = new[]
        {
            "models/ggml-tiny.bin",
            "models/ggml-base.bin",
            "models/ggml-small.bin"
        };

        foreach (var modelPath in models)
        {
            if (!File.Exists(modelPath))
            {
                Console.WriteLine($"??  跳過 {Path.GetFileName(modelPath)} (檔案不存在)\n");
                continue;
            }

            Console.WriteLine($"測試模型: {Path.GetFileName(modelPath)}");

            var config = new WhisperConfig
            {
                ModelPath = modelPath,
                Language = "zh"
            };

            var initStart = DateTime.Now;
            using var engine = new WhisperNetEngine();
            await engine.InitializeAsync(config);
            var initTime = (DateTime.Now - initStart).TotalMilliseconds;

            var segment = CreateMockAudioSegment();
            var result = await engine.TranscribeAsync(segment);

            Console.WriteLine($"  初始化: {initTime:F0}ms");
            Console.WriteLine($"  轉錄: {result.ProcessingTimeMs}ms");
            Console.WriteLine($"  文字: {result.Text.Substring(0, Math.Min(50, result.Text.Length))}...\n");
        }
    }

    /// <summary>
    /// 範例 9: 錯誤處理
    /// </summary>
    public static async Task ErrorHandlingExample()
    {
        Console.WriteLine("=== 範例 9: 錯誤處理 ===\n");

        // 錯誤 1: 模型檔案不存在
        try
        {
            var config = new WhisperConfig
            {
                ModelPath = "nonexistent.bin"
            };

            using var engine = new WhisperNetEngine();
            await engine.InitializeAsync(config);
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"? 捕獲錯誤: {ex.Message}\n");
        }

        // 錯誤 2: 未初始化就轉錄
        try
        {
            using var engine = new WhisperNetEngine();
            var segment = CreateMockAudioSegment();
            await engine.TranscribeAsync(segment);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"? 捕獲錯誤: {ex.Message}\n");
        }

        // 錯誤 3: 取消操作
        try
        {
            var config = new WhisperConfig
            {
                ModelPath = "models/ggml-base.bin"
            };

            using var engine = new WhisperNetEngine();
            await engine.InitializeAsync(config);

            var cts = new CancellationTokenSource();
            cts.Cancel();  // 立即取消

            var segment = CreateMockAudioSegment();
            await engine.TranscribeAsync(segment, cts.Token);
        }
        catch (OperationCanceledException ex)
        {
            Console.WriteLine($"? 捕獲取消: {ex.Message}\n");
        }
    }

    /// <summary>
    /// 範例 10: 完整工作流程
    /// </summary>
    public static async Task CompleteWorkflowExample()
    {
        Console.WriteLine("=== 範例 10: 完整工作流程 ===\n");

        // 1. 準備配置
        Console.WriteLine("1??  準備配置...");
        var config = new WhisperConfig
        {
            ModelPath = "models/ggml-base.bin",
            Language = "zh",
            EnableVAD = true,
            VadThreshold = 0.02f,
            SegmentDuration = 2.0,
            ThreadCount = Environment.ProcessorCount
        };
        Console.WriteLine($"   使用 {config.ThreadCount} 個執行緒\n");

        // 2. 初始化引擎
        Console.WriteLine("2??  初始化引擎...");
        using var engine = new WhisperNetEngine();
        await engine.InitializeAsync(config);
        Console.WriteLine("   ? 引擎就緒\n");

        // 3. 設定音訊擷取
        Console.WriteLine("3??  設定音訊擷取...");
        var vad = new VoiceActivityDetector(config.VadThreshold, config.MinSilenceDurationMs);
        using var audioCapture = new NAudioCapture(16000, 1, config.SegmentDuration, vad);
        Console.WriteLine("   ? 音訊擷取就緒\n");

        // 4. 建立轉錄服務
        Console.WriteLine("4??  建立轉錄服務...");
        using var service = new RealtimeTranscriptionService(engine, audioCapture);

        var transcriptionCount = 0;
        service.TranscriptionReceived += (sender, result) =>
        {
            transcriptionCount++;
            Console.WriteLine($"   [{transcriptionCount}] {result.Text}");
        };
        Console.WriteLine("   ? 服務就緒\n");

        // 5. 開始轉錄
        Console.WriteLine("5??  開始轉錄 (3 秒)...");
        var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);

        await Task.Delay(3000);  // 錄音 3 秒

        // 6. 停止並清理
        Console.WriteLine("\n6??  停止並清理...");
        await service.StopAsync();
        Console.WriteLine("   ? 完成\n");

        Console.WriteLine($"總共轉錄了 {transcriptionCount} 個片段");
    }

    // 輔助方法：建立模擬音訊片段
    private static AudioSegment CreateMockAudioSegment()
    {
        // 實際使用時，這應該是從麥克風或音訊檔案讀取的真實資料
        var samples = new float[16000];  // 1 秒的音訊 @ 16kHz
        var random = new Random();

        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (float)(random.NextDouble() * 0.1 - 0.05);  // 模擬低音量雜音
        }

        return new AudioSegment
        {
            Samples = samples,
            SampleRate = 16000,
            Channels = 1,
            StartTime = 0,
            Duration = 1.0,
            ContainsSpeech = true
        };
    }
}
