using System.Diagnostics;
using Whisper.net;
using Whisper.net.Ggml;
using WhisperTrans.Core.Interfaces;
using WhisperTrans.Core.Models;

namespace WhisperTrans.Core.Engines;

/// <summary>
/// Whisper.NET 引擎實現
/// 使用 whisper.net 函式庫進行語音轉錄
/// </summary>
public class WhisperNetEngine : IWhisperEngine
{
    private WhisperConfig? _config;
    private WhisperFactory? _whisperFactory;
    private WhisperProcessor? _processor;
    private bool _isInitialized;
    private bool _disposed;

    public bool IsInitialized => _isInitialized;
    public WhisperConfig? Config => _config;

    public async Task InitializeAsync(WhisperConfig config, CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            throw new InvalidOperationException("引擎已初始化");

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrEmpty(config.ModelPath))
            throw new ArgumentException("模型路徑不可為空", nameof(config));

        if (!File.Exists(config.ModelPath))
            throw new FileNotFoundException($"找不到模型檔案: {config.ModelPath}");

        _config = config;

        try
        {
            // 載入 Whisper 模型
            _whisperFactory = WhisperFactory.FromPath(config.ModelPath);
            
            // 建立處理器並配置參數
            var builder = _whisperFactory.CreateBuilder()
                .WithThreads(config.ThreadCount);

            // 設定語言（如果有指定）
            if (!string.IsNullOrEmpty(config.Language))
            {
                builder = builder.WithLanguage(config.Language);
            }

            // 設定翻譯模式
            if (config.Translate)
            {
                builder = builder.WithTranslate();
            }

            // 設定提示模式（自動提示可以提高準確度）
            builder = builder.WithPrompt("繁體中文語音轉錄。");

            _processor = builder.Build();

            await Task.CompletedTask;
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"初始化 Whisper 引擎失敗: {ex.Message}", ex);
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(AudioSegment segment, CancellationToken cancellationToken = default)
    {
        if (!_isInitialized || _processor == null)
            throw new InvalidOperationException("引擎尚未初始化");

        if (segment == null || segment.Samples == null || segment.Samples.Length == 0)
        {
            return new TranscriptionResult
            {
                Text = string.Empty,
                Language = _config?.Language ?? "unknown",
                Confidence = 0,
                ProcessingTimeMs = 0,
                Timestamp = segment?.StartTime ?? 0,
                IsFinal = true
            };
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Whisper.net 需要 float[] 格式的音訊資料
            var audioData = segment.Samples;

            // 如果啟用了 VAD 且沒有偵測到語音，直接返回空結果
            if (_config?.EnableVAD == true && !segment.ContainsSpeech)
            {
                return new TranscriptionResult
                {
                    Text = string.Empty,
                    Language = _config.Language ?? "unknown",
                    Confidence = 0,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Timestamp = segment.StartTime,
                    IsFinal = true
                };
            }

            // 執行轉錄
            var transcriptionText = string.Empty;
            var confidence = 0f;
            var detectedLanguage = _config?.Language ?? "unknown";

            await foreach (var result in _processor.ProcessAsync(audioData, cancellationToken))
            {
                transcriptionText += result.Text;
                
                // 計算平均信心度
                if (result.Probability > 0)
                {
                    confidence = Math.Max(confidence, (float)result.Probability);
                }

                // 獲取偵測到的語言
                if (!string.IsNullOrEmpty(result.Language))
                {
                    detectedLanguage = result.Language;
                }
            }

            stopwatch.Stop();

            return new TranscriptionResult
            {
                Text = transcriptionText.Trim(),
                Language = detectedLanguage,
                Confidence = confidence,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                Timestamp = segment.StartTime,
                IsFinal = true
            };
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // 記錄錯誤但不中斷處理流程
            Console.WriteLine($"轉錄錯誤: {ex.Message}");
            
            return new TranscriptionResult
            {
                Text = $"[轉錄錯誤: {ex.Message}]",
                Language = _config?.Language ?? "unknown",
                Confidence = 0,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                Timestamp = segment.StartTime,
                IsFinal = true
            };
        }
    }

    public async Task<IEnumerable<TranscriptionResult>> TranscribeBatchAsync(
        IEnumerable<AudioSegment> segments, 
        CancellationToken cancellationToken = default)
    {
        if (!_isInitialized)
            throw new InvalidOperationException("引擎尚未初始化");

        var results = new List<TranscriptionResult>();

        foreach (var segment in segments)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var result = await TranscribeAsync(segment, cancellationToken);
            results.Add(result);
        }

        return results;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _processor?.Dispose();
            _whisperFactory?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"清理 Whisper 資源時發生錯誤: {ex.Message}");
        }

        _processor = null;
        _whisperFactory = null;
        _disposed = true;
        _isInitialized = false;
        
        GC.SuppressFinalize(this);
    }
}
