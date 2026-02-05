namespace WhisperTrans.Core.Interfaces;

/// <summary>
/// 文字轉語音服務介面
/// </summary>
public interface ITextToSpeechService : IDisposable
{
    /// <summary>
    /// 是否正在播放
    /// </summary>
    bool IsSpeaking { get; }

    /// <summary>
    /// 取得可用的語音列表
    /// </summary>
    Task<IEnumerable<VoiceInfo>> GetAvailableVoicesAsync();

    /// <summary>
    /// 設定語音
    /// </summary>
    void SetVoice(string voiceId);

    /// <summary>
    /// 設定語速 (0.5 - 2.0)
    /// </summary>
    void SetRate(double rate);

    /// <summary>
    /// 設定音量 (0.0 - 1.0)
    /// </summary>
    void SetVolume(double volume);

    /// <summary>
    /// 朗讀文字
    /// </summary>
    Task SpeakAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止朗讀
    /// </summary>
    void Stop();

    /// <summary>
    /// 暫停朗讀
    /// </summary>
    void Pause();

    /// <summary>
    /// 繼續朗讀
    /// </summary>
    void Resume();
}

/// <summary>
/// 語音資訊
/// </summary>
public class VoiceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
