namespace WhisperTrans.Core.Models;

/// <summary>
/// 音訊裝置資訊
/// </summary>
public class AudioDeviceInfo
{
    /// <summary>
    /// 裝置索引
    /// </summary>
    public int DeviceIndex { get; set; }

    /// <summary>
    /// 裝置名稱
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// 裝置製造商
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// 聲道數量
    /// </summary>
    public int Channels { get; set; }

    /// <summary>
    /// 是否為預設裝置
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string DisplayName => IsDefault ? $"{DeviceName} (預設)" : DeviceName;

    public override string ToString() => DisplayName;
}
