# WhisperTrans 專案說明

## 目錄結構

```
WhisperTrans/
│
├── src/
│   ├── WhisperTrans.Core/              # 核心函式庫
│   │   ├── Models/                     # 資料模型
│   │   │   ├── TranscriptionResult.cs  # 轉錄結果
│   │   │   ├── AudioSegment.cs         # 音訊片段
│   │   │   └── WhisperConfig.cs        # Whisper 配置
│   │   │
│   │   ├── Interfaces/                 # 介面定義
│   │   │   ├── IWhisperEngine.cs       # Whisper 引擎介面
│   │   │   └── IAudioCapture.cs        # 音訊擷取介面
│   │   │
│   │   ├── Services/                   # 服務實現
│   │   │   ├── RealtimeTranscriptionService.cs  # 即時轉錄服務
│   │   │   └── VoiceActivityDetector.cs         # 語音活動檢測
│   │   │
│   │   ├── Engines/                    # Whisper 引擎實現
│   │   │   └── WhisperNetEngine.cs     # Whisper.NET 引擎
│   │   │
│   │   └── Audio/                      # 音訊處理
│   │       └── NAudioCapture.cs        # NAudio 音訊擷取
│   │
│   ├── WhisperTrans.Console/           # 控制台應用
│   │   └── Program.cs                  # 主程式
│   │
│   └── WhisperTrans.Desktop/           # WPF 桌面應用
│       ├── MainWindow.xaml             # 主視窗 UI
│       └── MainWindow.xaml.cs          # 主視窗邏輯
│
├── models/                             # Whisper 模型檔案（需自行下載）
│   └── ggml-base.bin
│
├── WhisperTrans.sln                    # Visual Studio 解決方案
├── appsettings.json                    # 應用配置檔
└── README.md                           # 專案說明文件
```

## 核心元件說明

### 1. WhisperTrans.Core (核心函式庫)

#### Models (資料模型)
- **TranscriptionResult**: 存儲轉錄結果，包含文字、語言、信心度、時間戳等資訊
- **AudioSegment**: 表示音訊片段，包含 PCM 樣本資料、採樣率、聲道數等
- **WhisperConfig**: Whisper 引擎配置，包括模型路徑、語言設定、GPU 選項等

#### Interfaces (介面)
- **IWhisperEngine**: 定義 Whisper 引擎的標準介面，支援不同的實現方式
- **IAudioCapture**: 定義音訊擷取介面，可以從麥克風或檔案讀取

#### Services (服務)
- **RealtimeTranscriptionService**: 
  - 核心即時轉錄服務
  - 實現滑動視窗機制
  - 管理音訊佇列和轉錄流程
  - 維護轉錄歷史記錄

- **VoiceActivityDetector (VAD)**:
  - 語音活動檢測器
  - 使用 RMS 能量分析
  - 自動偵測靜音片段
  - 支援在靜音處分割音訊

#### Engines (引擎實現)
- **WhisperNetEngine**: 
  - Whisper 引擎的基礎實現框架
  - 需要整合實際的 Whisper 庫（如 Whisper.NET）
  - 支援批次處理和單一片段轉錄

#### Audio (音訊處理)
- **NAudioCapture**:
  - 基於 NAudio 的音訊擷取實現
  - 支援即時麥克風錄音
  - 自動將 16-bit PCM 轉換為 float 格式
  - 整合 VAD 進行語音檢測

### 2. WhisperTrans.Console (控制台應用)

命令列介面應用程式，適合：
- 開發測試
- 自動化腳本
- 伺服器環境部署
- CI/CD 整合

功能：
- 互動式指令操作
- 即時顯示轉錄結果
- 支援歷史記錄管理
- 彩色輸出提升可讀性

### 3. WhisperTrans.Desktop (WPF 桌面應用)

Windows 桌面圖形介面，適合：
- 一般使用者
- 辦公室應用
- 會議記錄
- 語音筆記

功能：
- 友善的圖形介面
- 模型檔案瀏覽器
- 語言選擇與設定
- 即時顯示轉錄結果
- 匯出文字檔案

## 技術細節

### 滑動視窗實現

```csharp
// 音訊片段配置
SegmentDuration = 2.0 秒
SegmentOverlap = 0.5 秒

// 處理流程
時間 0.0s: 擷取片段 [0.0 - 2.0]
時間 1.5s: 擷取片段 [1.5 - 3.5]  // 與前一片段重疊 0.5 秒
時間 3.0s: 擷取片段 [3.0 - 5.0]
...
```

重疊設計的好處：
- 避免在語句邊界切斷造成辨識錯誤
- 保留上下文資訊
- 提高轉錄準確度

### VAD 檢測原理

```csharp
// RMS (Root Mean Square) 能量計算
RMS = sqrt(Σ(sample²) / sample_count)

// 如果 RMS > threshold，判定為包含語音
// 如果 RMS < threshold 持續 > minSilenceDuration，判定為靜音
```

優點：
- 節省計算資源（跳過靜音片段）
- 提高回應速度
- 減少無效轉錄

### 非同步處理架構

```
┌─────────────┐
│  麥克風輸入   │
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ NAudioCapture│ ← 100ms 緩衝
└──────┬──────┘
       │ AudioSegmentCaptured 事件
       ▼
┌─────────────────────┐
│ 音訊佇列 (Queue)     │
└──────┬──────────────┘
       │
       ▼
┌─────────────┐
│ VAD 檢測     │ ← 過濾靜音
└──────┬──────┘
       │
       ▼
┌─────────────┐
│ Whisper 引擎 │ ← 轉錄處理
└──────┬──────┘
       │
       ▼
┌─────────────────────┐
│ TranscriptionReceived│ ← 結果事件
└─────────────────────┘
```

所有步驟都是非同步執行，不會阻塞 UI 或主執行緒。

## NuGet 套件依賴

### WhisperTrans.Core
- **NAudio** (2.2.1): 音訊擷取和處理
- **Whisper.NET** (需手動安裝): Whisper 引擎綁定

### WhisperTrans.Console
- 參考 WhisperTrans.Core

### WhisperTrans.Desktop
- 參考 WhisperTrans.Core
- WPF 框架（.NET 10 內建）

## 下一步開發建議

### 短期目標
1. ✅ 整合 Whisper.NET 或 Whisper.cpp
2. ✅ 實現模型下載管理器
3. ✅ 添加設定檔讀取功能
4. ✅ 實現多語言切換

### 中期目標
1. 支援音訊檔案轉錄
2. 添加翻譯功能（語音轉文字 + 翻譯）
3. 實現即時字幕輸出（OBS 整合）
4. 支援多聲道分離轉錄

### 長期目標
1. 雲端模型支援（Azure Speech、Google Cloud Speech）
2. 自訂詞彙表和專有名詞優化
3. 說話人識別（Speaker Diarization）
4. 移動平台支援（Xamarin/MAUI）

## 效能優化建議

### GPU 加速
- NVIDIA GPU: 使用 CUDA 加速
- AMD GPU: 使用 ROCm 加速
- Apple Silicon: 使用 CoreML 加速

### 模型選擇
| 模型 | 大小 | 速度 | 準確度 | 適用場景 |
|------|------|------|--------|----------|
| tiny | 75 MB | 極快 | 一般 | 即時預覽、測試 |
| base | 140 MB | 快 | 良好 | 日常使用 |
| small | 460 MB | 中等 | 很好 | 專業應用 |
| medium | 1.5 GB | 慢 | 優秀 | 高品質需求 |
| large | 3 GB | 很慢 | 最佳 | 專業轉錄服務 |

### 記憶體優化
- 限制歷史記錄長度
- 定期清理音訊緩衝
- 使用物件池減少 GC 壓力

## 疑難排解

### 常見問題

**Q: 找不到模型檔案**
```
錯誤: 找不到模型檔案: models/ggml-base.bin
```
A: 請從 HuggingFace 下載模型並放置於 `models/` 目錄。

**Q: 麥克風無法錄音**
```
錯誤: NAudio 初始化失敗
```
A: 檢查麥克風權限設定（Windows 隱私設定）。

**Q: 轉錄結果為空或全是 [模擬轉錄結果]**
```
[模擬轉錄結果 - 請整合實際的 Whisper 引擎]
```
A: 需要整合實際的 Whisper 引擎，參考 README.md 的整合指南。

**Q: 處理速度太慢**
```
處理時間: 5000ms (超過音訊片段長度)
```
A: 
1. 啟用 GPU 加速
2. 使用較小的模型
3. 減少執行緒數量
4. 調整片段長度

---

**技術支援**: [GitHub Issues](https://github.com/your-repo/WhisperTrans/issues)  
**文件版本**: 1.0  
**最後更新**: 2026-02-04
