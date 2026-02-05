using System.Speech.Synthesis;
using WhisperTrans.Core.Interfaces;
using SpeechVoiceInfo = System.Speech.Synthesis.VoiceInfo;

namespace WhisperTrans.Core.Services;

/// <summary>
/// Windows 內建語音合成服務
/// </summary>
public class WindowsTtsService : ITextToSpeechService
{
    private readonly SpeechSynthesizer _synthesizer;
    private bool _disposed;

    public bool IsSpeaking => _synthesizer.State == SynthesizerState.Speaking;

    public WindowsTtsService()
    {
        _synthesizer = new SpeechSynthesizer();
        _synthesizer.SetOutputToDefaultAudioDevice();
    }

    public Task<IEnumerable<Interfaces.VoiceInfo>> GetAvailableVoicesAsync()
    {
        var voices = _synthesizer.GetInstalledVoices()
            .Where(v => v.Enabled)
            .Select(v => new Interfaces.VoiceInfo
            {
                Id = v.VoiceInfo.Name,
                Name = v.VoiceInfo.Name,
                Language = v.VoiceInfo.Culture.DisplayName,
                Gender = v.VoiceInfo.Gender.ToString(),
                Description = $"{v.VoiceInfo.Name} ({v.VoiceInfo.Culture.DisplayName})"
            });

        return Task.FromResult(voices);
    }

    public void SetVoice(string voiceId)
    {
        try
        {
            _synthesizer.SelectVoice(voiceId);
        }
        catch (ArgumentException)
        {
            // 如果找不到指定語音，使用預設語音
            System.Diagnostics.Debug.WriteLine($"無法找到語音: {voiceId}，使用預設語音");
        }
    }

    public void SetRate(double rate)
    {
        // Windows TTS rate 範圍: -10 到 10
        var windowsRate = (int)Math.Clamp((rate - 1.0) * 10, -10, 10);
        _synthesizer.Rate = windowsRate;
    }

    public void SetVolume(double volume)
    {
        // Windows TTS volume 範圍: 0 到 100
        var windowsVolume = (int)Math.Clamp(volume * 100, 0, 100);
        _synthesizer.Volume = windowsVolume;
    }

    public async Task SpeakAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var tcs = new TaskCompletionSource<bool>();

        void OnSpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            _synthesizer.SpeakCompleted -= OnSpeakCompleted;
            if (e.Cancelled)
                tcs.TrySetCanceled();
            else if (e.Error != null)
                tcs.TrySetException(e.Error);
            else
                tcs.TrySetResult(true);
        }

        _synthesizer.SpeakCompleted += OnSpeakCompleted;

        // 註冊取消
        cancellationToken.Register(() =>
        {
            _synthesizer.SpeakAsyncCancelAll();
            tcs.TrySetCanceled();
        });

        _synthesizer.SpeakAsync(text);
        await tcs.Task;
    }

    public void Stop()
    {
        _synthesizer.SpeakAsyncCancelAll();
    }

    public void Pause()
    {
        if (_synthesizer.State == SynthesizerState.Speaking)
        {
            _synthesizer.Pause();
        }
    }

    public void Resume()
    {
        if (_synthesizer.State == SynthesizerState.Paused)
        {
            _synthesizer.Resume();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _synthesizer?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
