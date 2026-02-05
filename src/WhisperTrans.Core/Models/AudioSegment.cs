namespace WhisperTrans.Core.Models;

/// <summary>
/// 音訊片段
/// </summary>
public class AudioSegment
{
    /// <summary>
    /// 音訊資料（PCM 格式）
    /// </summary>
    public float[] Samples { get; set; } = Array.Empty<float>();

    /// <summary>
    /// 採樣率 (Hz)
    /// </summary>
    public int SampleRate { get; set; } = 16000;

    /// <summary>
    /// 聲道數
    /// </summary>
    public int Channels { get; set; } = 1;

    /// <summary>
    /// 片段開始時間（秒）
    /// </summary>
    public double StartTime { get; set; }

    /// <summary>
    /// 片段持續時間（秒）
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// 是否包含語音（VAD 檢測結果）
    /// </summary>
    public bool ContainsSpeech { get; set; } = true;
}
