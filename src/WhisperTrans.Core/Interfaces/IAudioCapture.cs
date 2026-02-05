using WhisperTrans.Core.Models;

namespace WhisperTrans.Core.Interfaces;

/// <summary>
/// 音訊擷取介面
/// </summary>
public interface IAudioCapture : IDisposable
{
    /// <summary>
    /// 開始錄音
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 停止錄音
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// 音訊片段事件
    /// </summary>
    event EventHandler<AudioSegment>? AudioSegmentCaptured;

    /// <summary>
    /// 音訊層級變化事件
    /// </summary>
    event EventHandler<AudioLevelEventArgs>? AudioLevelChanged;

    /// <summary>
    /// 是否正在錄音
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// 採樣率
    /// </summary>
    int SampleRate { get; }
}
