# 遠端 Whisper ASR API 整合說明

## 功能概述

WhisperTrans v1.4.0 新增**遠端 Whisper ASR API** 支援，讓您可以選擇使用：

? **本地 Whisper.net** - 離線運作，隱私性高  
? **遠端 Whisper ASR API** - 伺服器運算，效能更強  

## API 資訊

### 遠端 ASR 服務

**端點**: `http://192.168.12.41:9000/asr`  
**方法**: `POST`  
**格式**: `multipart/form-data`

### 參數說明

| 參數 | 類型 | 必填 | 說明 |
|------|------|------|------|
| **audio_file** | file | ? | 音訊檔案 (WAV 格式) |
| **encode** | boolean | ? | 是否通過 ffmpeg 編碼 |
| **task** | string | ? | 任務類型 (transcribe/translate) |
| **language** | string | ? | 語言代碼 (zh, en, ja, 等) |
| **initial_prompt** | string | ? | 初始提示詞 |
| **output** | string | ? | 輸出格式 (txt, json, 等) |

### 請求範例

```bash
curl -X POST "http://192.168.12.41:9000/asr?encode=true&task=transcribe&language=zh&output=txt" \
  -F "audio_file=@audio.wav"
```

## 使用方式

### 方式 1: UI 操作

#### 步驟 1: 選擇引擎類型

```
┌──────────────────────────────────┐
│ Whisper 引擎:                    │
│ [遠端 API (Whisper ASR) ▼]       │
└──────────────────────────────────┘
```

1. 開啟 WhisperTrans
2. 在「配置」區域找到「Whisper 引擎」下拉選單
3. 選擇「遠端 API (Whisper ASR)」

#### 步驟 2: 設定 API URL

```
┌──────────────────────────────────┐
│ 遠端 API URL:                    │
│ http://192.168.12.41:9000/asr    │
│ [測試連線]                        │
└──────────────────────────────────┘
```

1. 輸入遠端 API URL: `http://192.168.12.41:9000/asr`
2. 點擊「測試連線」確認可用性
3. 看到「? 連線成功！」訊息

#### 步驟 3: 選擇語言

```
┌──────────────────────────────────┐
│ 語言:                            │
│ [繁體中文 ▼]                     │
└──────────────────────────────────┘
```

選擇您要轉錄的語言（自動偵測或指定語言）

#### 步驟 4: 初始化

```
[初始化]  ← 點擊
```

等待「遠端 Whisper ASR 已初始化」訊息

#### 步驟 5: 開始錄音

```
[?? 開始錄音]  ← 點擊
```

系統會將音訊發送到遠端 API 進行轉錄

### 方式 2: 切換引擎

#### 從本地切換到遠端

```
原本使用:
┌──────────────────────────────────┐
│ Whisper 引擎:                    │
│ [本地模型 (Whisper.net) ▼]       │
│                                  │
│ 模型路徑: models/ggml-base.bin   │
│ [瀏覽...] [下載模型...]          │
└──────────────────────────────────┘

切換後:
┌──────────────────────────────────┐
│ Whisper 引擎:                    │
│ [遠端 API (Whisper ASR) ▼]       │
│                                  │
│ 遠端 API URL:                    │
│ http://192.168.12.41:9000/asr    │
│ [測試連線]                        │
└──────────────────────────────────┘
```

UI 會自動切換顯示對應的配置選項

## 完整工作流程

### 遠端 ASR 模式

```
麥克風輸入
    ↓
NAudio 音訊捕捉
    ↓
VAD 語音檢測
    ↓
音訊片段 (WAV)
    ↓
HTTP POST → 遠端 ASR API
    ↓
接收文字結果
    ↓
顯示轉錄文字
    ↓
(選用) LLM 翻譯
    ↓
(選用) TTS 朗讀
```

### 本地模型模式

```
麥克風輸入
    ↓
NAudio 音訊捕捉
    ↓
VAD 語音檢測
    ↓
Whisper.net 本地轉錄
    ↓
顯示轉錄文字
    ↓
(選用) LLM 翻譯
    ↓
(選用) TTS 朗讀
```

## 技術實作

### 新增類別

#### RemoteWhisperEngine

```csharp
public class RemoteWhisperEngine : IWhisperEngine
{
    private readonly HttpClient _httpClient;
    private string _apiUrl;

    public async Task<TranscriptionResult> TranscribeAsync(
        AudioSegment segment, 
        CancellationToken cancellationToken = default)
    {
        // 1. 轉換為 WAV 格式
        var wavData = ConvertToWav(segment.Samples, segment.SampleRate);

        // 2. 建立 multipart/form-data
        using var content = new MultipartFormDataContent();
        var audioContent = new ByteArrayContent(wavData);
        content.Add(audioContent, "audio_file", "audio.wav");

        // 3. 發送請求
        var response = await _httpClient.PostAsync(requestUrl, content);
        var resultText = await response.Content.ReadAsStringAsync();

        // 4. 返回結果
        return new TranscriptionResult { Text = resultText };
    }
}
```

### 配置更新

#### WhisperConfig

```csharp
public class WhisperConfig
{
    // 新增屬性
    public WhisperEngineType EngineType { get; set; } // Local 或 Remote
    public string? RemoteApiUrl { get; set; }         // 遠端 API URL
    public string? InitialPrompt { get; set; }        // 初始提示詞

    // 原有屬性
    public string ModelPath { get; set; }             // 本地模型路徑
    public string? Language { get; set; }             // 語言代碼
    // ...
}

public enum WhisperEngineType
{
    Local,   // 本地 Whisper.net
    Remote   // 遠端 API
}
```

### UI 更新

#### MainWindow.xaml

```xaml
<!-- Whisper 引擎選擇 -->
<ComboBox x:Name="WhisperEngineComboBox" 
          SelectionChanged="WhisperEngineComboBox_SelectionChanged">
    <ComboBoxItem Content="本地模型 (Whisper.net)" Tag="local"/>
    <ComboBoxItem Content="遠端 API (Whisper ASR)" Tag="remote"/>
</ComboBox>

<!-- 遠端 API URL (動態顯示) -->
<TextBox x:Name="RemoteApiUrlTextBox" 
         Text="http://192.168.12.41:9000/asr"
         Visibility="Collapsed"/>
<Button x:Name="TestRemoteApiButton" 
        Click="TestRemoteApiButton_Click"
        Visibility="Collapsed"/>
```

## 效能對比

### 本地 Whisper.net

| 項目 | 數值 |
|------|------|
| **初始化時間** | 5-10 秒 (載入模型) |
| **轉錄延遲** | 200-500 ms |
| **CPU 使用** | 50-80% (單核) |
| **GPU 使用** | 30-60% (如果啟用) |
| **記憶體** | 1-3 GB |
| **網路需求** | ? 不需要 |
| **隱私性** | ????? |

### 遠端 ASR API

| 項目 | 數值 |
|------|------|
| **初始化時間** | < 1 秒 |
| **轉錄延遲** | 100-300 ms + 網路延遲 |
| **CPU 使用** | < 5% |
| **GPU 使用** | 0% (本地) |
| **記憶體** | < 100 MB |
| **網路需求** | ? 需要 |
| **隱私性** | ??? |

## 使用場景

### 推薦使用本地模型

? **需要離線運作**  
? **注重隱私安全**  
? **網路不穩定**  
? **單機使用**  

### 推薦使用遠端 API

? **需要高效能轉錄**  
? **多用戶端共享**  
? **網路環境良好**  
? **伺服器資源豐富**  
? **電腦效能有限**  

## 測試連線

### 自動測試

點擊「測試連線」按鈕：

```
測試流程:
1. 生成 1 秒靜音測試音訊
2. 轉換為 WAV 格式
3. 發送到遠端 API
4. 接收並解析回應
5. 顯示結果
```

### 成功訊息

```
? 連線成功！

遠端 API 可以正常使用。

API URL: http://192.168.12.41:9000/asr
處理時間: 234ms
```

### 失敗訊息

```
連線失敗:

[錯誤訊息]

請檢查：
1. API URL 是否正確
2. 遠端服務是否已啟動
3. 網路連線是否正常
4. 防火牆設定
```

## 手動測試

### 使用 curl

```bash
# 測試基本連線
curl -X POST "http://192.168.12.41:9000/asr?task=transcribe&output=txt" \
  -F "audio_file=@test.wav"

# 測試繁體中文
curl -X POST "http://192.168.12.41:9000/asr?language=zh&output=txt" \
  -F "audio_file=@chinese.wav"

# 測試編碼
curl -X POST "http://192.168.12.41:9000/asr?encode=true&output=txt" \
  -F "audio_file=@audio.mp3"
```

### 使用 PowerShell

```powershell
# 準備請求
$url = "http://192.168.12.41:9000/asr?task=transcribe&output=txt"
$file = "test.wav"

# 發送請求
$response = Invoke-WebRequest -Uri $url -Method POST -Form @{
    audio_file = Get-Item $file
}

# 顯示結果
$response.Content
```

## 常見問題

### Q1: 如何切換引擎？

**A**: 
1. 停止錄音（如果正在錄音）
2. 在「Whisper 引擎」下拉選單選擇
3. 設定對應參數
4. 重新初始化

### Q2: 遠端 API 需要 API Key 嗎？

**A**: 取決於您的遠端服務設定。
- 內網服務通常不需要
- 公網服務建議使用驗證

### Q3: 可以同時使用本地和遠端嗎？

**A**: 目前一次只能使用一種引擎。
但可以快速切換。

### Q4: 遠端 API 支援哪些語言？

**A**: 取決於遠端服務部署的模型。
常見支援：
- 繁體中文 (zh)
- 英文 (en)
- 日文 (ja)
- 韓文 (ko)
- 等等

### Q5: 網路斷線會怎樣？

**A**: 
- 轉錄會失敗
- 顯示錯誤訊息
- 可切換回本地模型

### Q6: 音訊數據安全嗎？

**A**: 
- 內網 API: 資料不離開區域網路
- 公網 API: 建議使用 HTTPS
- 本地模型: 資料完全不離開電腦

## 疑難排解

### 問題 1: 連線超時

**症狀**: `The operation has timed out`

**解決**:
1. 檢查網路連線
2. 確認 API 服務運行中
3. 增加超時時間（程式碼修改）
4. 檢查防火牆

### 問題 2: HTTP 400 Bad Request

**症狀**: `HTTP 400`

**原因**: 
- 音訊格式不正確
- 參數設定錯誤

**解決**:
1. 確認音訊為 16kHz 單聲道
2. 檢查 URL 參數
3. 查看伺服器日誌

### 問題 3: HTTP 500 Internal Server Error

**症狀**: `HTTP 500`

**原因**: 遠端服務內部錯誤

**解決**:
1. 檢查遠端服務日誌
2. 確認模型已正確載入
3. 重啟遠端服務
4. 聯繫管理員

### 問題 4: 轉錄結果為空

**症狀**: 返回空字串

**原因**:
- 音訊太短
- 沒有偵測到語音
- VAD 設定過於嚴格

**解決**:
1. 延長錄音時間
2. 調整 VAD 閾值
3. 測試不同音訊

## 部署遠端 ASR 服務

### 使用 Faster Whisper

```python
# server.py
from faster_whisper import WhisperModel
from fastapi import FastAPI, File, UploadFile
import uvicorn

app = FastAPI()
model = WhisperModel("large-v3", device="cuda")

@app.post("/asr")
async def transcribe(
    audio_file: UploadFile = File(...),
    language: str = "zh",
    task: str = "transcribe"
):
    # 讀取音訊
    audio_bytes = await audio_file.read()
    
    # 轉錄
    segments, info = model.transcribe(
        audio_bytes,
        language=language,
        task=task
    )
    
    # 返回文字
    text = " ".join([seg.text for seg in segments])
    return text

# 啟動
uvicorn.run(app, host="0.0.0.0", port=9000)
```

### 啟動服務

```bash
# 安裝依賴
pip install faster-whisper fastapi uvicorn python-multipart

# 執行
python server.py
```

### Docker 部署

```dockerfile
FROM python:3.10

WORKDIR /app

RUN pip install faster-whisper fastapi uvicorn python-multipart

COPY server.py .

EXPOSE 9000

CMD ["python", "server.py"]
```

```bash
# 建置
docker build -t whisper-asr .

# 執行
docker run -p 9000:9000 --gpus all whisper-asr
```

## 相關資源

- [Faster Whisper](https://github.com/guillaumekln/faster-whisper)
- [FastAPI 文件](https://fastapi.tiangolo.com/)
- [Whisper API 說明](./Whisper引擎整合說明.md)
- [快速使用指南](./快速使用指南.md)

---

**版本**: 1.4.0  
**最後更新**: 2024  
**狀態**: ? 已實作
