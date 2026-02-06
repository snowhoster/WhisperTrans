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
    private WhisperConfig? _config;
    private bool _disposed;
    private string _apiUrl = string.Empty;

    public bool IsInitialized { get; private set; }
    public WhisperConfig? Config => _config;

    public RemoteWhisperEngine()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public Task InitializeAsync(WhisperConfig config, CancellationToken cancellationToken = default)
    {
        _config = config;
        
        // 檢查遠端 API URL 是否設定
        if (string.IsNullOrWhiteSpace(config.RemoteApiUrl))
        {
            throw new ArgumentException("遠端 API URL 未設定", nameof(config));
        }

        _apiUrl = config.RemoteApiUrl.TrimEnd('/');
        
        // 確保 API URL 包含 /asr 端點
        if (!_apiUrl.EndsWith("/asr", StringComparison.OrdinalIgnoreCase))
        {
            _apiUrl += "/asr";
        }

        IsInitialized = true;
        
        System.Diagnostics.Debug.WriteLine($"遠端 Whisper ASR 已初始化: {_apiUrl}");
        
        return Task.CompletedTask;
    }

    public async Task<TranscriptionResult> TranscribeAsync(AudioSegment segment, CancellationToken cancellationToken = default)
    {
        if (!IsInitialized || _config == null)
        {
            throw new InvalidOperationException("引擎尚未初始化");
        }

        var startTime = DateTime.Now;

        try
        {
            // 將音訊數據轉換為 WAV 格式
            var wavData = ConvertToWav(segment.Samples, segment.SampleRate);

            // 準備 multipart/form-data
            using var content = new MultipartFormDataContent();
            
            // 添加音訊檔案
            var audioContent = new ByteArrayContent(wavData);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            content.Add(audioContent, "audio_file", "audio.wav");

            // 建立請求 URL with query parameters
            var requestUrl = BuildRequestUrl(_apiUrl, _config);

            System.Diagnostics.Debug.WriteLine($"發送 ASR 請求: {requestUrl}");
            System.Diagnostics.Debug.WriteLine($"音訊大小: {wavData.Length} bytes");

            // 發送請求
            var response = await _httpClient.PostAsync(requestUrl, content, cancellationToken);
            
            // 檢查回應
            response.EnsureSuccessStatusCode();

            // 讀取回應
            var resultText = await response.Content.ReadAsStringAsync(cancellationToken);
            
            var processingTime = (DateTime.Now - startTime).TotalMilliseconds;

            System.Diagnostics.Debug.WriteLine($"ASR 回應: {resultText}");
            System.Diagnostics.Debug.WriteLine($"處理時間: {processingTime}ms");

            return new TranscriptionResult
            {
                Text = resultText.Trim(),
                Timestamp = segment.StartTime,
                ProcessingTimeMs = (long)processingTime,
                Confidence = 1.0f, // 遠端 API 通常不提供信心度
                IsFinal = true,
                Language = _config.Language ?? "unknown"
            };
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"ASR HTTP 錯誤: {ex.Message}");
            throw new Exception($"遠端 ASR 請求失敗: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ASR 錯誤: {ex.Message}");
            throw;
        }
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
        var queryParams = new List<string>();

        // encode 參數
        queryParams.Add("encode=true");

        // task 參數 (transcribe 或 translate)
        queryParams.Add("task=transcribe");

        // language 參數
        if (!string.IsNullOrWhiteSpace(config.Language))
        {
            queryParams.Add($"language={config.Language}");
        }

        // initial_prompt 參數（如果需要）
        if (!string.IsNullOrWhiteSpace(config.InitialPrompt))
        {
            queryParams.Add($"initial_prompt={Uri.EscapeDataString(config.InitialPrompt)}");
        }

        // output 參數
        queryParams.Add("output=txt");

        return $"{baseUrl}?{string.Join("&", queryParams)}";
    }

    private byte[] ConvertToWav(float[] audioData, int sampleRate)
    {
        // 將 float32 音訊數據轉換為 int16 PCM
        var pcmData = new short[audioData.Length];
        for (int i = 0; i < audioData.Length; i++)
        {
            var sample = audioData[i];
            
            // 限制範圍在 -1.0 到 1.0
            sample = Math.Clamp(sample, -1.0f, 1.0f);
            
            // 轉換為 int16
            pcmData[i] = (short)(sample * short.MaxValue);
        }

        // 建立 WAV 檔案
        using var memoryStream = new MemoryStream();
        using var writer = new BinaryWriter(memoryStream);

        // WAV 檔頭
        var channels = 1;
        var bitsPerSample = 16;
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = channels * bitsPerSample / 8;
        var dataSize = pcmData.Length * 2;

        // RIFF header
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));

        // fmt chunk
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16); // chunk size
        writer.Write((short)1); // audio format (PCM)
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);

        // data chunk
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);
        
        // 寫入 PCM 數據
        foreach (var sample in pcmData)
        {
            writer.Write(sample);
        }

        return memoryStream.ToArray();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _httpClient?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
