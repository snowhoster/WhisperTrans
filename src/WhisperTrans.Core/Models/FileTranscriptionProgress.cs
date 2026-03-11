namespace WhisperTrans.Core.Models;

/// <summary>
/// 檔案轉錄進度
/// </summary>
public class FileTranscriptionProgress
{
    public int CurrentChunk { get; set; }
    public int TotalChunks { get; set; }
    public double ProgressPercent { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsIndeterminate { get; set; }
}
