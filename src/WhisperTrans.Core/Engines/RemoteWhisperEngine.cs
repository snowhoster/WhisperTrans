using System.Net.Http.Headers;
using System.Text;
using WhisperTrans.Core.Interfaces;
using WhisperTrans.Core.Models;

namespace WhisperTrans.Core.Engines;

/// <summary>
/// 遠端 Whisper ASR API 引擎
/// </summary>
public class RemoteWhisperEngine : IWhisperEngine
{
    private readonly HttpClient _httpClient;
    private readonly HttpClient _fileHttpClient; // 長 timeout，用於檔案上傳
    private WhisperConfig? _config;
    private bool _disposed;
    private string _apiUrl = string.Empty;

    public bool IsInitialized { get; private set; }
    public WhisperConfig? Config => _config;

    public RemoteWhisperEngine()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _fileHttpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    public Task InitializeAsync(WhisperConfig config, CancellationToken cancellationToken = default)
    {
        _config = config;

        if (string.IsNullOrWhiteSpace(config.RemoteApiUrl))
            throw new ArgumentException("遠端 API URL 未設定", nameof(config));

        _apiUrl = config.RemoteApiUrl.TrimEnd('/');

        if (!_apiUrl.EndsWith("/asr", StringComparison.OrdinalIgnoreCase))
            _apiUrl += "/asr";

        IsInitialized = true;

        System.Diagnostics.Debug.WriteLine($"遠端 Whisper ASR 已初始化: {_apiUrl}");

        return Task.CompletedTask;
    }

    /// <summary>
    /// 轉錄音訊片段（即時錄音路徑，傳入 float[] 再轉 WAV）
    /// </summary>
    public async Task<TranscriptionResult> TranscribeAsync(AudioSegment segment, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized || _config == null)
            throw new InvalidOperationException("引擎尚未初始化");

        var startTime = DateTime.Now;

        var wavData = ConvertToWav(segment.Samples, segment.SampleRate);

        using var content = new MultipartFormDataContent();
        var audioContent = new ByteArrayContent(wavData);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        content.Add(audioContent, "audio_file", "audio.wav");

        var requestUrl = BuildRequestUrl(_apiUrl, _config);

        System.Diagnostics.Debug.WriteLine($"ASR 請求: {requestUrl} ({wavData.Length} bytes)");

        var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var resultText = await response.Content.ReadAsStringAsync(cancellationToken);
        var processingTime = (DateTime.Now - startTime).TotalMilliseconds;

        System.Diagnostics.Debug.WriteLine($"ASR 結果: {resultText} ({processingTime:F0}ms)");

        return new TranscriptionResult
        {
            Text = resultText.Trim(),
            Timestamp = segment.StartTime,
            ProcessingTimeMs = (long)processingTime,
            Confidence = 1.0f,
            IsFinal = true,
            Language = _config.Language ?? "unknown"
        };
    }

    /// <summary>
    /// 直接上傳音訊檔案（MP3、WAV 等），由遠端 ffmpeg 處理格式轉換
    /// </summary>
    public async Task<string> TranscribeRawFileAsync(
        string filePath,
        IProgress<FileTranscriptionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsInitialized || _config == null)
            throw new InvalidOperationException("引擎尚未初始化");

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"找不到檔案：{filePath}");

        progress?.Report(new FileTranscriptionProgress
        {
            Message = "上傳檔案至遠端 API...",
            IsIndeterminate = true,
            ProgressPercent = 0
        });

        var fileName = Path.GetFileName(filePath);
        var mimeType = GetMimeType(filePath);

        var fileBytes = await File.ReadAllBytesAsync(filePath, cancellationToken);

        using var content = new MultipartFormDataContent();
        var audioContent = new ByteArrayContent(fileBytes);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
        content.Add(audioContent, "audio_file", fileName);

        // encode=true 讓遠端伺服器用 ffmpeg 處理格式
        var requestUrl = BuildRequestUrl(_apiUrl, _config);

        System.Diagnostics.Debug.WriteLine($"上傳檔案: {fileName} ({fileBytes.Length / 1024}KB) → {requestUrl}");

        var startTime = DateTime.Now;
        var response = await _fileHttpClient.PostAsync(requestUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var resultText = await response.Content.ReadAsStringAsync(cancellationToken);
        var elapsed = (DateTime.Now - startTime).TotalMilliseconds;

        System.Diagnostics.Debug.WriteLine($"遠端轉錄完成 ({elapsed:F0}ms): {resultText}");

        progress?.Report(new FileTranscriptionProgress
        {
            Message = "轉錄完成",
            IsIndeterminate = false,
            ProgressPercent = 100,
            CurrentChunk = 1,
            TotalChunks = 1
        });

        return resultText.Trim();
    }

    public async Task<IEnumerable<TranscriptionResult>> TranscribeBatchAsync(IEnumerable<AudioSegment> segments, CancellationToken cancellationToken = default)
    {
        var results = new List<TranscriptionResult>();
        foreach (var segment in segments)
        {
            var result = await TranscribeAsync(segment, cancellationToken);
            results.Add(result);
        }
        return results;
    }

    private string BuildRequestUrl(string baseUrl, WhisperConfig config)
    {
        var queryParams = new List<string> { "encode=true", "task=transcribe" };

        if (!string.IsNullOrWhiteSpace(config.Language))
            queryParams.Add($"language={config.Language}");

        if (!string.IsNullOrWhiteSpace(config.InitialPrompt))
            queryParams.Add($"initial_prompt={Uri.EscapeDataString(config.InitialPrompt)}");

        queryParams.Add("output=txt");

        return $"{baseUrl}?{string.Join("&", queryParams)}";
    }

    private static byte[] ConvertToWav(float[] audioData, int sampleRate)
    {
        var pcmData = new short[audioData.Length];
        for (var i = 0; i < audioData.Length; i++)
        {
            var sample = Math.Clamp(audioData[i], -1.0f, 1.0f);
            pcmData[i] = (short)(sample * short.MaxValue);
        }

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        const int channels = 1;
        const int bitsPerSample = 16;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var dataSize = pcmData.Length * 2;

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);
        foreach (var sample in pcmData)
            writer.Write(sample);

        return ms.ToArray();
    }

    private static string GetMimeType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".m4a" => "audio/mp4",
            ".aac" => "audio/aac",
            ".ogg" => "audio/ogg",
            ".wma" => "audio/x-ms-wma",
            ".flac" => "audio/flac",
            _ => "application/octet-stream"
        };

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient?.Dispose();
        _fileHttpClient?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
