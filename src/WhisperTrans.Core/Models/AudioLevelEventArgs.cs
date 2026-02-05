namespace WhisperTrans.Core.Models;

/// <summary>
/// 音訊層級資訊
/// </summary>
public class AudioLevelEventArgs : EventArgs
{
    /// <summary>
    /// 音訊層級 (0.0 - 1.0)
    /// </summary>
    public float Level { get; set; }

    /// <summary>
    /// 峰值層級 (0.0 - 1.0)
    /// </summary>
    public float PeakLevel { get; set; }

    /// <summary>
    /// 是否偵測到語音
    /// </summary>
    public bool IsSpeechDetected { get; set; }

    /// <summary>
    /// 時間戳記
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
