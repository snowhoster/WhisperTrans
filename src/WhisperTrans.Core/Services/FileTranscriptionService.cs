using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Text;
using WhisperTrans.Core.Engines;
using WhisperTrans.Core.Interfaces;
using WhisperTrans.Core.Models;

namespace WhisperTrans.Core.Services;

/// <summary>
/// 音訊檔案轉錄服務，支援 MP3、WAV、M4A 等格式
/// </summary>
public class FileTranscriptionService
{
    private const int TargetSampleRate = 16000;
    private const int ChunkSeconds = 30;

    private readonly IWhisperEngine _engine;

    public FileTranscriptionService(IWhisperEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <summary>
    /// 轉錄音訊檔案
    /// </summary>
    public async Task<string> TranscribeFileAsync(
        string filePath,
        IProgress<FileTranscriptionProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"找不到音訊檔案：{filePath}");

        // 遠端引擎：直接上傳原始檔案，由伺服器的 ffmpeg 處理格式轉換
        if (_engine is RemoteWhisperEngine remoteEngine)
        {
            return await remoteEngine.TranscribeRawFileAsync(filePath, progress, cancellationToken);
        }

        // 本地引擎：用 NAudio 解碼成 16kHz mono float[]，再分段轉錄
        return await TranscribeLocalAsync(filePath, progress, cancellationToken);
    }

    private async Task<string> TranscribeLocalAsync(
        string filePath,
        IProgress<FileTranscriptionProgress>? progress,
        CancellationToken cancellationToken)
    {
        progress?.Report(new FileTranscriptionProgress
        {
            Message = "解碼音訊檔案中...",
            IsIndeterminate = true,
            ProgressPercent = 0
        });

        var samples = LoadAudioFile(filePath, out var totalDuration);

        var chunkSize = TargetSampleRate * ChunkSeconds;
        var totalChunks = (int)Math.Ceiling((double)samples.Length / chunkSize);
        var result = new StringBuilder();

        for (var i = 0; i < totalChunks; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var start = i * chunkSize;
            var length = Math.Min(chunkSize, samples.Length - start);
            var chunkSamples = samples[start..(start + length)];
            var startTime = (double)start / TargetSampleRate;

            progress?.Report(new FileTranscriptionProgress
            {
                CurrentChunk = i + 1,
                TotalChunks = totalChunks,
                ProgressPercent = (double)(i + 1) / totalChunks * 100,
                Message = $"轉錄第 {i + 1}/{totalChunks} 段 ({startTime:F0}s)...",
                IsIndeterminate = false
            });

            var segment = new AudioSegment
            {
                Samples = chunkSamples,
                SampleRate = TargetSampleRate,
                StartTime = startTime,
                Duration = (double)length / TargetSampleRate,
                ContainsSpeech = true
            };

            var transcription = await _engine.TranscribeAsync(segment, cancellationToken);

            if (!string.IsNullOrWhiteSpace(transcription.Text))
            {
                result.AppendLine(transcription.Text.Trim());
            }
        }

        return result.ToString().Trim();
    }

    /// <summary>
    /// 使用 NAudio 讀取音訊檔案，輸出為 16kHz mono float[]
    /// </summary>
    private static float[] LoadAudioFile(string filePath, out double totalDuration)
    {
        using var reader = new AudioFileReader(filePath);
        totalDuration = reader.TotalTime.TotalSeconds;

        // 轉為單聲道
        ISampleProvider provider = reader.WaveFormat.Channels == 1
            ? reader
            : new StereoToMonoSampleProvider(reader);

        // 重新取樣到 16kHz
        if (provider.WaveFormat.SampleRate != TargetSampleRate)
        {
            provider = new WdlResamplingSampleProvider(provider, TargetSampleRate);
        }

        var samples = new List<float>();
        var buffer = new float[TargetSampleRate]; // 1 秒的緩衝區
        int read;
        while ((read = provider.Read(buffer, 0, buffer.Length)) > 0)
        {
            samples.AddRange(buffer.AsSpan(0, read));
        }

        return [.. samples];
    }

    /// <summary>
    /// 取得支援的副檔名清單
    /// </summary>
    public static string GetSupportedExtensionsFilter()
        => "音訊檔案 (*.mp3;*.wav;*.m4a;*.aac;*.wma;*.flac;*.ogg)|*.mp3;*.wav;*.m4a;*.aac;*.wma;*.flac;*.ogg|所有檔案 (*.*)|*.*";
}
