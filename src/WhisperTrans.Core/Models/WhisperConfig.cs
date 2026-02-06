namespace WhisperTrans.Core.Models;

/// <summary>
/// Whisper 引擎配置
/// </summary>
public class WhisperConfig
{
    /// <summary>
    /// 引擎類型：Local（本地 Whisper.net）或 Remote（遠端 API）
    /// </summary>
    public WhisperEngineType EngineType { get; set; } = WhisperEngineType.Local;

    /// <summary>
    /// 模型路徑（本地引擎使用）
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;

    /// <summary>
    /// 遠端 API URL（遠端引擎使用）
    /// </summary>
    public string? RemoteApiUrl { get; set; }

    /// <summary>
    /// 語言代碼（如 "zh", "en"），null 為自動偵測
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// 使用 GPU 加速（本地引擎使用）
    /// </summary>
    public bool UseGpu { get; set; } = true;

    /// <summary>
    /// 執行緒數量（本地引擎使用）
    /// </summary>
    public int ThreadCount { get; set; } = Environment.ProcessorCount;

    /// <summary>
    /// 音訊片段長度（秒）
    /// </summary>
    public double SegmentDuration { get; set; } = 2.0;

    /// <summary>
    /// 片段重疊時間（秒）
    /// </summary>
    public double SegmentOverlap { get; set; } = 0.5;

    /// <summary>
    /// 啟用語音活動檢測（VAD）
    /// </summary>
    public bool EnableVAD { get; set; } = true;

    /// <summary>
    /// VAD 靜音閾值（0-1）
    /// </summary>
    public float VadThreshold { get; set; } = 0.5f;

    /// <summary>
    /// 最小靜音持續時間（毫秒）
    /// </summary>
    public int MinSilenceDurationMs { get; set; } = 500;

    /// <summary>
    /// 翻譯為英文（本地引擎使用）
    /// </summary>
    public bool Translate { get; set; } = false;

    /// <summary>
    /// 初始提示詞（遠端引擎使用）
    /// </summary>
    public string? InitialPrompt { get; set; }
}

/// <summary>
/// Whisper 引擎類型
/// </summary>
public enum WhisperEngineType
{
    /// <summary>
    /// 本地 Whisper.net 引擎
    /// </summary>
    Local,

    /// <summary>
    /// 遠端 Whisper ASR API
    /// </summary>
    Remote
}
