using System.Net.Http;

namespace WhisperTrans.Core.Services;

/// <summary>
/// Whisper 家更狝叭
/// </summary>
public class ModelDownloader
{
    private readonly HttpClient _httpClient;
    
    // Whisper 家更 URL 琈甮
    private static readonly Dictionary<string, string> ModelUrls = new()
    {
        { "ggml-tiny.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin" },
        { "ggml-base.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin" },
        { "ggml-small.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin" },
        { "ggml-medium.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin" },
        { "ggml-large-v1.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v1.bin" },
        { "ggml-large-v2.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v2.bin" },
        { "ggml-large-v3.bin", "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin" }
    };

    public ModelDownloader()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(30); // 郎惠璶耕丁
    }

    /// <summary>
    /// 更家郎
    /// </summary>
    /// <param name="modelName">家嘿 "ggml-base.bin"</param>
    /// <param name="outputPath">块隔畖</param>
    /// <param name="progress">更秈厨0-100</param>
    /// <param name="cancellationToken">礟</param>
    public async Task DownloadModelAsync(
        string modelName,
        string outputPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!ModelUrls.TryGetValue(modelName, out var url))
        {
            throw new ArgumentException($"ゼ家嘿: {modelName}や穿家: {string.Join(", ", ModelUrls.Keys)}");
        }

        // 絋玂ヘ魁
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        progress?.Report(new DownloadProgress { Status = "タ硈钡狝竟...", PercentComplete = 0 });

        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var canReportProgress = totalBytes != -1;

        progress?.Report(new DownloadProgress 
        { 
            Status = $"秨﹍更 {modelName}...", 
            PercentComplete = 0,
            TotalBytes = totalBytes,
            DownloadedBytes = 0
        });

        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        var totalRead = 0L;
        var lastReportedPercent = 0;

        while (true)
        {
            var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            if (bytesRead == 0)
                break;

            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalRead += bytesRead;

            if (canReportProgress && progress != null)
            {
                var percentComplete = (int)((totalRead * 100) / totalBytes);
                if (percentComplete != lastReportedPercent)
                {
                    lastReportedPercent = percentComplete;
                    progress.Report(new DownloadProgress
                    {
                        Status = $"更い {modelName}...",
                        PercentComplete = percentComplete,
                        TotalBytes = totalBytes,
                        DownloadedBytes = totalRead
                    });
                }
            }
        }

        progress?.Report(new DownloadProgress 
        { 
            Status = "更ЧΘ!", 
            PercentComplete = 100,
            TotalBytes = totalBytes,
            DownloadedBytes = totalRead
        });
    }

    /// <summary>
    /// 浪琩更家狦ぃ
    /// </summary>
    public async Task EnsureModelExistsAsync(
        string modelPath,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (File.Exists(modelPath))
        {
            progress?.Report(new DownloadProgress 
            { 
                Status = "家郎", 
                PercentComplete = 100 
            });
            return;
        }

        var fileName = Path.GetFileName(modelPath);
        await DownloadModelAsync(fileName, modelPath, progress, cancellationToken);
    }

    /// <summary>
    /// 眔や穿家
    /// </summary>
    public static IEnumerable<string> GetAvailableModels()
    {
        return ModelUrls.Keys;
    }

    /// <summary>
    /// 眔家箇︳MB
    /// </summary>
    public static long GetEstimatedModelSize(string modelName)
    {
        return modelName switch
        {
            "ggml-tiny.bin" => 75,
            "ggml-base.bin" => 142,
            "ggml-small.bin" => 466,
            "ggml-medium.bin" => 1464,
            "ggml-large-v1.bin" => 2950,
            "ggml-large-v2.bin" => 2950,
            "ggml-large-v3.bin" => 2950,
            _ => 0
        };
    }
}

/// <summary>
/// 更秈戈癟
/// </summary>
public class DownloadProgress
{
    /// <summary>
    /// 篈癟
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// ЧΘκだゑ0-100
    /// </summary>
    public int PercentComplete { get; set; }

    /// <summary>
    /// 羆じ舱计
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// 更じ舱计
    /// </summary>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// Αて秈癟
    /// </summary>
    public string FormattedMessage
    {
        get
        {
            if (TotalBytes > 0)
            {
                var totalMB = TotalBytes / (1024.0 * 1024.0);
                var downloadedMB = DownloadedBytes / (1024.0 * 1024.0);
                return $"{Status} - {downloadedMB:F2} MB / {totalMB:F2} MB ({PercentComplete}%)";
            }
            return $"{Status} ({PercentComplete}%)";
        }
    }
}
