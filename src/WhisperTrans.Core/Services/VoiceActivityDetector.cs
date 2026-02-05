namespace WhisperTrans.Core.Services;

/// <summary>
/// 語音活動檢測器（VAD）- 用於檢測音訊中是否包含語音
/// </summary>
public class VoiceActivityDetector
{
    private readonly float _threshold;
    private readonly int _minSilenceSamples;

    public VoiceActivityDetector(float threshold = 0.5f, int minSilenceDurationMs = 500, int sampleRate = 16000)
    {
        _threshold = threshold;
        _minSilenceSamples = (minSilenceDurationMs * sampleRate) / 1000;
    }

    /// <summary>
    /// 檢測音訊片段是否包含語音
    /// </summary>
    public bool ContainsSpeech(float[] samples)
    {
        if (samples == null || samples.Length == 0)
            return false;

        // 計算 RMS (Root Mean Square) 能量
        double sumSquares = 0;
        for (int i = 0; i < samples.Length; i++)
        {
            sumSquares += samples[i] * samples[i];
        }
        
        double rms = Math.Sqrt(sumSquares / samples.Length);

        // 如果 RMS 高於閾值，認為包含語音
        return rms > _threshold;
    }

    /// <summary>
    /// 檢測靜音片段的位置
    /// </summary>
    public List<(int start, int end)> DetectSilenceRegions(float[] samples)
    {
        var silenceRegions = new List<(int, int)>();
        int silenceStart = -1;

        for (int i = 0; i < samples.Length; i++)
        {
            bool isSilence = Math.Abs(samples[i]) < _threshold;

            if (isSilence && silenceStart == -1)
            {
                silenceStart = i;
            }
            else if (!isSilence && silenceStart != -1)
            {
                int silenceLength = i - silenceStart;
                if (silenceLength >= _minSilenceSamples)
                {
                    silenceRegions.Add((silenceStart, i));
                }
                silenceStart = -1;
            }
        }

        // 處理最後的靜音片段
        if (silenceStart != -1 && (samples.Length - silenceStart) >= _minSilenceSamples)
        {
            silenceRegions.Add((silenceStart, samples.Length));
        }

        return silenceRegions;
    }

    /// <summary>
    /// 在靜音處分割音訊
    /// </summary>
    public List<float[]> SplitOnSilence(float[] samples)
    {
        var silenceRegions = DetectSilenceRegions(samples);
        var segments = new List<float[]>();

        if (silenceRegions.Count == 0)
        {
            segments.Add(samples);
            return segments;
        }

        int lastEnd = 0;
        foreach (var (start, end) in silenceRegions)
        {
            if (start > lastEnd)
            {
                var segment = new float[start - lastEnd];
                Array.Copy(samples, lastEnd, segment, 0, segment.Length);
                segments.Add(segment);
            }
            lastEnd = end;
        }

        // 添加最後一個片段
        if (lastEnd < samples.Length)
        {
            var segment = new float[samples.Length - lastEnd];
            Array.Copy(samples, lastEnd, segment, 0, segment.Length);
            segments.Add(segment);
        }

        return segments;
    }
}
