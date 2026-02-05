# 大語言模型翻譯功能說明

## 功能概述

WhisperTrans 現在整合了大語言模型（LLM）翻譯功能，可以即時將語音轉錄結果翻譯成指定語言。

## 主要特色

? **即時翻譯** - 每轉錄一句，自動翻譯一句  
?? **多語言支援** - 支援翻譯成 9 種常用語言  
?? **多平台支援** - OpenAI、Azure、Claude、Gemini、Ollama  
?? **靈活配置** - 自訂 API URL、模型名稱  
?? **安全設計** - API Key 使用密碼輸入  
?? **連線測試** - 驗證 API 設定是否正確  

## 界面說明

### 右側翻譯面板

```
┌─────────────────────────────────────┐
│ ?? 大語言模型翻譯                   │
├─────────────────────────────────────┤
│                                     │
│ ? 啟用即時翻譯                      │
│                                     │
│ LLM 提供商: [OpenAI (GPT) ▼]       │
│                                     │
│ API URL:                            │
│ [https://api.openai.com/v1/...]    │
│                                     │
│ API Key:                            │
│ [●●●●●●●●●●●●●●●●]                  │
│                                     │
│ 翻譯成: [繁體中文 ▼]                │
│                                     │
│ 模型名稱:                           │
│ [gpt-3.5-turbo]                     │
│                                     │
│ [?? 測試連線]                       │
├─────────────────────────────────────┤
│ ?? 翻譯結果                         │
│ ┌─────────────────────────────────┐ │
│ │ [00:00:05] 你好，這是測試。     │ │
│ │ [00:00:12] 系統運作正常。       │ │
│ │                                 │ │
│ └─────────────────────────────────┘ │
├─────────────────────────────────────┤
│ [清除翻譯]  [匯出翻譯]             │
└─────────────────────────────────────┘
```

## 支援的 LLM 提供商

### 1. OpenAI (GPT) ?
**預設配置：**
- API URL: `https://api.openai.com/v1/chat/completions`
- 模型: `gpt-3.5-turbo` 或 `gpt-4`
- 費用: 按使用量計費
- 註冊: https://platform.openai.com/

**優點：**
- 翻譯品質優秀
- API 穩定可靠
- 文件完善

**範例 API Key:**
```
sk-proj-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

### 2. Azure OpenAI
**預設配置：**
- API URL: `https://YOUR-RESOURCE.openai.azure.com/openai/deployments/YOUR-DEPLOYMENT/chat/completions?api-version=2024-02-15-preview`
- 模型: `gpt-35-turbo`
- 費用: 企業級定價
- 註冊: https://azure.microsoft.com/

**適合：**
- 企業用戶
- 需要資料隱私保護
- 中國大陸地區使用

### 3. Anthropic (Claude)
**預設配置：**
- API URL: `https://api.anthropic.com/v1/messages`
- 模型: `claude-3-sonnet-20240229`
- 費用: 按使用量計費
- 註冊: https://www.anthropic.com/

**優點：**
- 支援長文本
- 翻譯更自然

### 4. Google (Gemini)
**預設配置：**
- API URL: `https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent`
- 模型: `gemini-pro`
- 費用: 有免費額度
- 註冊: https://ai.google.dev/

**優點：**
- 免費額度較大
- 多語言支援好

### 5. 本地模型 (Ollama)
**預設配置：**
- API URL: `http://localhost:11434/api/chat`
- 模型: `llama2`、`mistral`
- 費用: 完全免費
- 安裝: https://ollama.ai/

**優點：**
- 完全免費
- 資料不外傳
- 無需網路

**缺點：**
- 需要本地硬體資源
- 翻譯品質較低

### 6. 自訂 API
支援任何相容 OpenAI API 格式的服務。

## 翻譯語言支援

| 語言 | 語言代碼 | 說明 |
|------|----------|------|
| 繁體中文 | zh-TW | Traditional Chinese |
| 簡體中文 | zh-CN | Simplified Chinese |
| 英文 | en | English |
| 日文 | ja | Japanese |
| 韓文 | ko | Korean |
| 法文 | fr | French |
| 德文 | de | German |
| 西班牙文 | es | Spanish |
| 俄文 | ru | Russian |

## 使用流程

### 基本設定

#### 步驟 1：選擇 LLM 提供商
```
1. 點擊 "LLM 提供商" 下拉選單
2. 選擇您使用的服務（如 OpenAI）
3. 系統會自動填入預設 API URL 和模型名稱
```

#### 步驟 2：輸入 API 憑證
```
1. 在 "API URL" 欄位確認或修改端點
2. 在 "API Key" 欄位輸入您的 API 金鑰
3. 如需更改，修改 "模型名稱"
```

#### 步驟 3：選擇翻譯目標語言
```
1. 點擊 "翻譯成" 下拉選單
2. 選擇目標語言（預設：繁體中文）
```

#### 步驟 4：測試連線
```
1. 點擊 "?? 測試連線" 按鈕
2. 系統會發送測試請求
3. 確認連線成功後即可使用
```

#### 步驟 5：啟用翻譯
```
1. 勾選 "? 啟用即時翻譯"
2. 開始錄音
3. 每次轉錄完成會自動翻譯
```

### 進階設定

#### OpenAI 設定範例
```
LLM 提供商: OpenAI (GPT)
API URL: https://api.openai.com/v1/chat/completions
API Key: sk-proj-xxxxx...
翻譯成: 繁體中文
模型名稱: gpt-3.5-turbo
```

**成本參考（GPT-3.5-turbo）：**
- 輸入: $0.0015 / 1K tokens
- 輸出: $0.002 / 1K tokens
- 每句約 0.5-2K tokens
- 每句翻譯約 $0.001-0.004

#### Azure OpenAI 設定範例
```
LLM 提供商: Azure OpenAI
API URL: https://myresource.openai.azure.com/openai/deployments/gpt35/chat/completions?api-version=2024-02-15-preview
API Key: your-azure-api-key
翻譯成: 簡體中文
模型名稱: gpt-35-turbo
```

**特殊設定：**
- 需要替換 `myresource` 為您的資源名稱
- 需要替換 `gpt35` 為您的部署名稱
- API 版本可能需要更新

#### Ollama 本地模型設定範例
```
LLM 提供商: 本地模型 (Ollama)
API URL: http://localhost:11434/api/chat
API Key: (留空)
翻譯成: 英文
模型名稱: llama2
```

**安裝 Ollama：**
```bash
# Windows
winget install Ollama.Ollama

# macOS
brew install ollama

# Linux
curl -fsSL https://ollama.ai/install.sh | sh

# 下載模型
ollama pull llama2
ollama pull mistral
```

## 工作流程

### 即時翻譯流程

```
┌─────────────────┐
│  使用者說話     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Whisper 轉錄   │
│  "Hello World"  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  LLM 翻譯       │
│  "你好世界"     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  顯示翻譯結果   │
└─────────────────┘
```

### 翻譯提示詞範例

系統會自動生成類似以下的提示詞：

```
System: 你是一個專業的翻譯助手，專門將文字翻譯成繁體中文。

User: 請將以下文字翻譯成繁體中文，只回傳翻譯結果，不要其他說明：

Hello, this is a test.
```

**LLM 回應：**
```
你好，這是一個測試。
```

## 功能操作

### 清除翻譯
```
點擊 "清除翻譯" 按鈕：
- 清空翻譯結果區域
- 不影響原始轉錄結果
- 可以重新開始翻譯
```

### 匯出翻譯
```
點擊 "匯出翻譯" 按鈕：
1. 選擇儲存位置
2. 設定檔案名稱（預設：翻譯_日期時間.txt）
3. 儲存為純文字檔案
4. 包含時間戳記
```

**匯出格式範例：**
```txt
[00:00:05] 你好，這是一段測試語音。
[00:00:12] 系統正在即時轉錄並翻譯。
[00:00:18] 翻譯品質非常好。
```

## 技術細節

### API 請求格式

**OpenAI API 請求：**
```json
{
  "model": "gpt-3.5-turbo",
  "messages": [
    {
      "role": "system",
      "content": "你是一個專業的翻譯助手，專門將文字翻譯成繁體中文。"
    },
    {
      "role": "user",
      "content": "請將以下文字翻譯成繁體中文，只回傳翻譯結果，不要其他說明：\n\nHello World"
    }
  ],
  "temperature": 0.3,
  "max_tokens": 1000
}
```

**回應格式：**
```json
{
  "choices": [
    {
      "message": {
        "content": "你好世界"
      }
    }
  ]
}
```

### 錯誤處理

系統會捕獲並處理以下錯誤：

1. **API 連線錯誤**
   ```
   [翻譯錯誤: 無法連接到 API]
   ```

2. **認證失敗**
   ```
   [翻譯錯誤: API Key 無效]
   ```

3. **超時錯誤**
   ```
   [翻譯錯誤: 請求超時]
   ```

4. **配額用盡**
   ```
   [翻譯錯誤: API 配額已用盡]
   ```

### 效能優化

**翻譯參數：**
```csharp
temperature = 0.3    // 較低的隨機性，翻譯更一致
max_tokens = 1000    // 最大回應長度
timeout = 30秒       // 請求超時時間
```

**非同步處理：**
- 翻譯不會阻塞轉錄
- 使用 async/await 模式
- 錯誤不影響主流程

## 使用範例

### 範例 1：中翻英
```
設定：
  翻譯成: 英文
  
轉錄: 你好，今天天氣很好。
翻譯: Hello, the weather is nice today.
```

### 範例 2：英翻中
```
設定：
  翻譯成: 繁體中文
  
轉錄: Hello, how are you?
翻譯: 你好，你好嗎？
```

### 範例 3：日翻中
```
設定：
  翻譯成: 繁體中文
  
轉錄: ?????、元????？
翻譯: 你好，你好嗎？
```

## 最佳實踐

### 1. API Key 安全
```
? 不要將 API Key 提交到版本控制
? 定期更換 API Key
? 使用環境變數存儲敏感資訊
? 設定 API 使用限額
```

### 2. 成本控制
```
? 使用 GPT-3.5-turbo 而非 GPT-4（成本低 10 倍）
? 設定 max_tokens 限制
? 監控 API 使用量
? 考慮使用 Ollama 本地模型
```

### 3. 翻譯品質
```
? 選擇合適的目標語言
? 確保原文清晰準確
? 使用較新的模型
? 調整 temperature 參數
```

### 4. 錯誤處理
```
? 測試 API 連線
? 準備備用 API
? 檢查網路連線
? 監控錯誤日誌
```

## 疑難排解

### Q: 翻譯速度很慢
**A**: 
1. 檢查網路連線速度
2. 嘗試使用更快的模型（GPT-3.5 vs GPT-4）
3. 考慮使用本地 Ollama 模型
4. 減少翻譯文字長度

### Q: API Key 無效
**A**:
1. 確認 API Key 正確複製
2. 檢查 API Key 是否過期
3. 確認帳戶有可用額度
4. 重新生成 API Key

### Q: 翻譯結果不準確
**A**:
1. 使用更好的模型（GPT-4）
2. 檢查原始轉錄是否準確
3. 調整溫度參數（降低到 0.1-0.3）
4. 優化提示詞

### Q: 無法連接到 API
**A**:
1. 檢查 API URL 是否正確
2. 確認防火牆設定
3. 測試網路連線
4. 檢查服務商狀態頁面

### Q: Azure OpenAI 連線失敗
**A**:
1. 確認資源名稱正確
2. 檢查部署名稱
3. 更新 API 版本
4. 確認 API Key 格式

## 進階功能

### 自訂提示詞

可以修改 `LLMTranslationService.cs` 來自訂提示詞：

```csharp
var prompt = $"請將以下文字翻譯成{languageName}，保持專業術語：\n\n{text}";
```

### 批次翻譯

未來可以實作批次翻譯功能：

```csharp
public async Task<List<string>> TranslateBatchAsync(List<string> texts)
{
    // 將多句合併翻譯，降低API調用次數
}
```

### 翻譯記憶

可以加入翻譯記憶功能：

```csharp
private Dictionary<string, string> _translationCache = new();
```

## 成本估算

### GPT-3.5-turbo
```
假設：
- 每句平均 1000 tokens（500 輸入 + 500 輸出）
- 價格：$0.0015/1K input + $0.002/1K output

每句成本：
  輸入: 500 * $0.0015 / 1000 = $0.00075
  輸出: 500 * $0.002 / 1000 = $0.001
  總計: $0.00175

1小時轉錄（約 1800 句）：
  成本: 1800 * $0.00175 = $3.15
```

### Ollama（免費）
```
成本: $0
硬體需求: 8GB RAM + GPU（可選）
```

## 更新日誌

### v1.2.0 - LLM 翻譯功能
- ? 新增即時翻譯功能
- ? 支援 6 種 LLM 提供商
- ? 支援 9 種目標語言
- ? API 連線測試功能
- ? 翻譯結果匯出
- ? 完整的錯誤處理

## 相關資源

- [OpenAI API 文件](https://platform.openai.com/docs)
- [Azure OpenAI 文件](https://learn.microsoft.com/azure/ai-services/openai/)
- [Anthropic API 文件](https://docs.anthropic.com/)
- [Google AI 文件](https://ai.google.dev/docs)
- [Ollama 文件](https://ollama.ai/docs)

---

**版本**: 1.2.0  
**最後更新**: 2024
