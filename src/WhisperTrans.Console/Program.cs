using WhisperTrans.Core.Audio;
using WhisperTrans.Core.Engines;
using WhisperTrans.Core.Models;
using WhisperTrans.Core.Services;

Console.WriteLine("===================================");
Console.WriteLine("  WhisperTrans 即時語音轉文字系統");
Console.WriteLine("===================================");
Console.WriteLine();

// 檢查模型檔案路徑
Console.Write("請輸入 Whisper 模型路徑 (或按 Enter 使用預設路徑): ");
var modelPath = Console.ReadLine();

if (string.IsNullOrWhiteSpace(modelPath))
{
    modelPath = Path.Combine(AppContext.BaseDirectory, "models", "ggml-base.bin");
    Console.WriteLine($"使用預設路徑: {modelPath}");
}

if (!File.Exists(modelPath))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\n找不到模型檔案: {modelPath}");
    Console.ResetColor();
    
    Console.Write("\n是否要自動下載模型？ (Y/N): ");
    var response = Console.ReadLine()?.Trim().ToUpper();
    
    if (response == "Y" || response == "YES")
    {
        try
        {
            var modelDownloader = new ModelDownloader();
            var modelName = Path.GetFileName(modelPath);
            var estimatedSize = ModelDownloader.GetEstimatedModelSize(modelName);
            
            Console.WriteLine($"\n準備下載: {modelName}");
            Console.WriteLine($"預估大小: {estimatedSize} MB");
            Console.WriteLine("這可能需要幾分鐘時間，請耐心等候...\n");

            var progress = new Progress<DownloadProgress>(p =>
            {
                Console.Write($"\r{p.FormattedMessage}");
            });

            await modelDownloader.EnsureModelExistsAsync(modelPath, progress);
            
            Console.WriteLine("\n\n✓ 模型下載完成!");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n\n模型下載失敗: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("\n請手動下載 Whisper 模型檔案:");
            Console.WriteLine("1. 訪問: https://huggingface.co/ggerganov/whisper.cpp");
            Console.WriteLine("2. 下載模型 (推薦: ggml-base.bin 或 ggml-small.bin)");
            Console.WriteLine("3. 將檔案放置於: models/ 目錄下");
            Console.WriteLine("\n按任意鍵退出...");
            Console.ReadKey();
            return;
        }
    }
    else
    {
        Console.WriteLine("\n請手動下載 Whisper 模型檔案:");
        Console.WriteLine("1. 訪問: https://huggingface.co/ggerganov/whisper.cpp");
        Console.WriteLine("2. 下載模型 (推薦: ggml-base.bin 或 ggml-small.bin)");
        Console.WriteLine("3. 將檔案放置於: models/ 目錄下");
        Console.WriteLine("\n按任意鍵退出...");
        Console.ReadKey();
        return;
    }
}

// 配置 Whisper
var config = new WhisperConfig
{
    ModelPath = modelPath,
    Language = "zh", // 中文
    UseGpu = true,
    SegmentDuration = 2.0,
    SegmentOverlap = 0.5,
    EnableVAD = true,
    VadThreshold = 0.02f,
    MinSilenceDurationMs = 500
};

Console.WriteLine("\n初始化中...");

try
{
    // 建立引擎和音訊擷取
    using var whisperEngine = new WhisperNetEngine();
    await whisperEngine.InitializeAsync(config);

    var vad = new VoiceActivityDetector(
        threshold: config.VadThreshold,
        minSilenceDurationMs: config.MinSilenceDurationMs
    );

    using var audioCapture = new NAudioCapture(
        sampleRate: 16000,
        channels: 1,
        segmentDuration: config.SegmentDuration,
        vad: vad
    );

    using var transcriptionService = new RealtimeTranscriptionService(whisperEngine, audioCapture);

    // 註冊事件處理
    transcriptionService.TranscriptionReceived += (sender, result) =>
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{result.Timestamp:F2}s] {result.Text}");
        Console.WriteLine($"  語言: {result.Language} | 信心度: {result.Confidence:P0} | 處理時間: {result.ProcessingTimeMs}ms");
        Console.ResetColor();
    };

    transcriptionService.PartialTranscriptionReceived += (sender, text) =>
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[部分] {text}");
        Console.ResetColor();
    };

    transcriptionService.ErrorOccurred += (sender, ex) =>
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"錯誤: {ex.Message}");
        Console.ResetColor();
    };

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("\n✓ 初始化完成!");
    Console.WriteLine("\n[指令]");
    Console.WriteLine("  Enter - 開始/停止錄音");
    Console.WriteLine("  C     - 清除轉錄歷史");
    Console.WriteLine("  S     - 顯示完整轉錄");
    Console.WriteLine("  Q     - 退出");
    Console.ResetColor();

    var cts = new CancellationTokenSource();
    bool isRecording = false;

    while (true)
    {
        Console.Write("\n> ");
        var key = Console.ReadKey(intercept: true);
        Console.WriteLine();

        switch (key.Key)
        {
            case ConsoleKey.Enter:
                if (!isRecording)
                {
                    Console.WriteLine("🎤 開始錄音...");
                    await transcriptionService.StartAsync(cts.Token);
                    isRecording = true;
                }
                else
                {
                    Console.WriteLine("⏸️  停止錄音...");
                    await transcriptionService.StopAsync();
                    isRecording = false;
                }
                break;

            case ConsoleKey.C:
                transcriptionService.ClearHistory();
                Console.WriteLine("✓ 歷史記錄已清除");
                break;

            case ConsoleKey.S:
                var fullText = transcriptionService.GetFullTranscription();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\n完整轉錄:");
                Console.WriteLine(new string('=', 60));
                Console.WriteLine(fullText);
                Console.WriteLine(new string('=', 60));
                Console.ResetColor();
                break;

            case ConsoleKey.Q:
                if (isRecording)
                {
                    await transcriptionService.StopAsync();
                }
                Console.WriteLine("再見!");
                return;

            default:
                Console.WriteLine("無效指令");
                break;
        }
    }
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n發生錯誤: {ex.Message}");
    Console.WriteLine($"\n堆疊追蹤:\n{ex.StackTrace}");
    Console.ResetColor();
    Console.WriteLine("\n按任意鍵退出...");
    Console.ReadKey();
}
