using WhisperTrans.Core.Models;

namespace WhisperTrans.Core.Interfaces;

/// <summary>
/// Whisper 語音轉文字引擎介面
/// </summary>
public interface IWhisperEngine : IDisposable
{
    /// <summary>
    /// 初始化引擎
    /// </summary>
    Task InitializeAsync(WhisperConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// 轉錄單一音訊片段
    /// </summary>
    Task<TranscriptionResult> TranscribeAsync(AudioSegment segment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批次轉錄多個音訊片段
    /// </summary>
    Task<IEnumerable<TranscriptionResult>> TranscribeBatchAsync(IEnumerable<AudioSegment> segments, CancellationToken cancellationToken = default);

    /// <summary>
    /// 引擎是否已初始化
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// 當前配置
    /// </summary>
    WhisperConfig? Config { get; }
}
