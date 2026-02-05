using NAudio.Wave;
using WhisperTrans.Core.Interfaces;
using WhisperTrans.Core.Models;
using WhisperTrans.Core.Services;

namespace WhisperTrans.Core.Audio;

/// <summary>
/// NAudio 音訊擷取實現
/// 使用 NAudio 庫進行麥克風錄音
/// </summary>
public class NAudioCapture : IAudioCapture
{
    private WaveInEvent? _waveIn;
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly double _segmentDuration;
    private readonly VoiceActivityDetector? _vad;
    private readonly List<float> _buffer = new();
    private bool _isRecording;
    private double _currentTime;
    private int _deviceNumber = -1; // -1 表示使用預設裝置

    public event EventHandler<AudioSegment>? AudioSegmentCaptured;
    public event EventHandler<AudioLevelEventArgs>? AudioLevelChanged;
    
    public bool IsRecording => _isRecording;
    public int SampleRate => _sampleRate;

    public NAudioCapture(int sampleRate = 16000, int channels = 1, double segmentDuration = 2.0, VoiceActivityDetector? vad = null, int deviceNumber = -1)
    {
        _sampleRate = sampleRate;
        _channels = channels;
        _segmentDuration = segmentDuration;
        _vad = vad;
        _deviceNumber = deviceNumber;
    }

    /// <summary>
    /// 設定音訊輸入裝置
    /// </summary>
    public void SetDevice(int deviceNumber)
    {
        if (_isRecording)
            throw new InvalidOperationException("無法在錄音中變更裝置");
        
        _deviceNumber = deviceNumber;
    }

    /// <summary>
    /// 取得可用的音訊輸入裝置列表
    /// </summary>
    public static List<AudioDeviceInfo> GetAvailableDevices()
    {
        var devices = new List<AudioDeviceInfo>();
        int deviceCount = WaveInEvent.DeviceCount;

        for (int i = 0; i < deviceCount; i++)
        {
            var capabilities = WaveInEvent.GetCapabilities(i);
            devices.Add(new AudioDeviceInfo
            {
                DeviceIndex = i,
                DeviceName = capabilities.ProductName,
                Manufacturer = capabilities.ManufacturerGuid.ToString(),
                Channels = capabilities.Channels,
                IsDefault = i == 0 // 第一個裝置通常是預設裝置
            });
        }

        return devices;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRecording)
            throw new InvalidOperationException("已在錄音中");

        _waveIn = new WaveInEvent
        {
            DeviceNumber = _deviceNumber,
            WaveFormat = new WaveFormat(_sampleRate, 16, _channels),
            BufferMilliseconds = 100
        };

        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _buffer.Clear();
        _currentTime = 0;
        _isRecording = true;

        _waveIn.StartRecording();

        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        if (!_isRecording || _waveIn == null)
            return Task.CompletedTask;

        _isRecording = false;
        _waveIn.StopRecording();

        // 處理剩餘的緩衝資料
        if (_buffer.Count > 0)
        {
            ProcessBuffer();
        }

        return Task.CompletedTask;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (!_isRecording)
            return;

        // 將 16-bit PCM 轉換為 float
        var samples = ConvertBytesToFloats(e.Buffer, e.BytesRecorded);
        _buffer.AddRange(samples);

        // 計算音訊層級
        CalculateAndRaiseAudioLevel(samples);

        // 檢查是否達到片段長度
        var samplesPerSegment = (int)(_sampleRate * _segmentDuration);
        if (_buffer.Count >= samplesPerSegment)
        {
            ProcessBuffer();
        }
    }

    private void CalculateAndRaiseAudioLevel(float[] samples)
    {
        if (samples.Length == 0)
            return;

        // 計算 RMS（均方根）音量
        float sum = 0;
        float peak = 0;
        
        foreach (var sample in samples)
        {
            var abs = Math.Abs(sample);
            sum += sample * sample;
            if (abs > peak)
                peak = abs;
        }

        float rms = (float)Math.Sqrt(sum / samples.Length);
        
        // 檢查是否有語音
        bool isSpeech = _vad?.ContainsSpeech(samples) ?? (rms > 0.02f);

        AudioLevelChanged?.Invoke(this, new AudioLevelEventArgs
        {
            Level = rms,
            PeakLevel = peak,
            IsSpeechDetected = isSpeech
        });
    }

    private void ProcessBuffer()
    {
        var samples = _buffer.ToArray();
        _buffer.Clear();

        // 執行 VAD 檢測
        bool containsSpeech = _vad?.ContainsSpeech(samples) ?? true;

        var segment = new AudioSegment
        {
            Samples = samples,
            SampleRate = _sampleRate,
            Channels = _channels,
            StartTime = _currentTime,
            Duration = samples.Length / (double)_sampleRate,
            ContainsSpeech = containsSpeech
        };

        _currentTime += segment.Duration;

        AudioSegmentCaptured?.Invoke(this, segment);
    }

    private float[] ConvertBytesToFloats(byte[] buffer, int bytesRecorded)
    {
        var sampleCount = bytesRecorded / 2; // 16-bit = 2 bytes per sample
        var floats = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = (short)((buffer[i * 2 + 1] << 8) | buffer[i * 2]);
            floats[i] = sample / 32768f; // 標準化到 -1.0 ~ 1.0
        }

        return floats;
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        _isRecording = false;

        if (e.Exception != null)
        {
            // 記錄錯誤
            Console.WriteLine($"錄音錯誤: {e.Exception.Message}");
        }
    }

    public void Dispose()
    {
        if (_isRecording)
        {
            StopAsync().GetAwaiter().GetResult();
        }

        if (_waveIn != null)
        {
            _waveIn.DataAvailable -= OnDataAvailable;
            _waveIn.RecordingStopped -= OnRecordingStopped;
            _waveIn.Dispose();
            _waveIn = null;
        }

        GC.SuppressFinalize(this);
    }
}
