namespace WhisperTrans.Core.Models;

/// <summary>
/// 轉錄結果
/// </summary>
public class TranscriptionResult
{
    /// <summary>
    /// 轉錄的文字內容
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// 偵測到的語言
    /// </summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>
    /// 信心度 (0-1)
    /// </summary>
    public float Confidence { get; set; }

    /// <summary>
    /// 處理時間（毫秒）
    /// </summary>
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// 音訊片段時間戳（秒）
    /// </summary>
    public double Timestamp { get; set; }

    /// <summary>
    /// 是否為最終結果（非中間預測）
    /// </summary>
    public bool IsFinal { get; set; }
}
