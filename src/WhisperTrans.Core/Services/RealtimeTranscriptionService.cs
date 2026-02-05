using System.Collections.Concurrent;
using WhisperTrans.Core.Interfaces;
using WhisperTrans.Core.Models;

namespace WhisperTrans.Core.Services;

/// <summary>
/// 即時轉錄服務 - 實現滑動視窗機制
/// </summary>
public class RealtimeTranscriptionService : IDisposable
{
    private readonly IWhisperEngine _whisperEngine;
    private readonly IAudioCapture _audioCapture;
    private readonly ConcurrentQueue<AudioSegment> _audioQueue = new();
    private readonly SemaphoreSlim _processingLock = new(1, 1);
    private CancellationTokenSource? _cts;
    private Task? _processingTask;
    private bool _isRunning;
    private readonly List<string> _transcriptionHistory = new();
    private const int MaxHistoryLength = 10; // 保留最近 10 段轉錄結果作為上下文

    public event EventHandler<TranscriptionResult>? TranscriptionReceived;
    public event EventHandler<string>? PartialTranscriptionReceived;
    public event EventHandler<Exception>? ErrorOccurred;

    public bool IsRunning => _isRunning;

    public RealtimeTranscriptionService(IWhisperEngine whisperEngine, IAudioCapture audioCapture)
    {
        _whisperEngine = whisperEngine ?? throw new ArgumentNullException(nameof(whisperEngine));
        _audioCapture = audioCapture ?? throw new ArgumentNullException(nameof(audioCapture));
        _audioCapture.AudioSegmentCaptured += OnAudioSegmentCaptured;
    }

    /// <summary>
    /// 啟動即時轉錄
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning)
            throw new InvalidOperationException("轉錄服務已在執行中");

        if (!_whisperEngine.IsInitialized)
            throw new InvalidOperationException("Whisper 引擎尚未初始化");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isRunning = true;

        // 啟動音訊擷取
        await _audioCapture.StartAsync(_cts.Token);

        // 啟動處理任務
        _processingTask = ProcessAudioQueueAsync(_cts.Token);
    }

    /// <summary>
    /// 停止即時轉錄
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        _isRunning = false;
        _cts?.Cancel();

        await _audioCapture.StopAsync();

        if (_processingTask != null)
            await _processingTask;

        _cts?.Dispose();
        _cts = null;
    }

    private void OnAudioSegmentCaptured(object? sender, AudioSegment segment)
    {
        if (!_isRunning)
            return;

        // 加入佇列等待處理
        _audioQueue.Enqueue(segment);
    }

    private async Task ProcessAudioQueueAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (_audioQueue.TryDequeue(out var segment))
                {
                    await ProcessAudioSegmentAsync(segment, cancellationToken);
                }
                else
                {
                    // 佇列為空，短暫休眠
                    await Task.Delay(10, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }
        }
    }

    private async Task ProcessAudioSegmentAsync(AudioSegment segment, CancellationToken cancellationToken)
    {
        await _processingLock.WaitAsync(cancellationToken);
        try
        {
            // 如果啟用 VAD 且此片段不包含語音，跳過
            if (_whisperEngine.Config?.EnableVAD == true && !segment.ContainsSpeech)
            {
                return;
            }

            // 執行轉錄
            var result = await _whisperEngine.TranscribeAsync(segment, cancellationToken);

            if (!string.IsNullOrWhiteSpace(result.Text))
            {
                // 更新歷史記錄（保持上下文）
                UpdateTranscriptionHistory(result.Text);

                // 觸發事件
                if (result.IsFinal)
                {
                    TranscriptionReceived?.Invoke(this, result);
                }
                else
                {
                    PartialTranscriptionReceived?.Invoke(this, result.Text);
                }
            }
        }
        finally
        {
            _processingLock.Release();
        }
    }

    private void UpdateTranscriptionHistory(string text)
    {
        _transcriptionHistory.Add(text);
        
        // 保持歷史記錄在最大長度內
        while (_transcriptionHistory.Count > MaxHistoryLength)
        {
            _transcriptionHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// 獲取完整的轉錄歷史
    /// </summary>
    public string GetFullTranscription()
    {
        return string.Join(" ", _transcriptionHistory);
    }

    /// <summary>
    /// 清除轉錄歷史
    /// </summary>
    public void ClearHistory()
    {
        _transcriptionHistory.Clear();
    }

    public void Dispose()
    {
        if (_isRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }

        _processingLock.Dispose();
        GC.SuppressFinalize(this);
    }
}
