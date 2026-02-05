# API Key 設定與儲存功能說明

## 新增功能

v1.2.2 版本新增了方便的 API Key 設定和儲存功能，解決了無法預設 API Key 和模型設定的問題。

## 功能特色

### 1. API Key 顯示/隱藏切換

- **隱藏模式（預設）**: 使用 PasswordBox，API Key 顯示為 ●●●●●
- **顯示模式**: 使用 TextBox，API Key 明文顯示
- **快速切換**: 點擊「??? 顯示」核取方塊即可切換

### 2. 設定儲存/載入

- **?? 儲存設定**: 將當前的 API 配置儲存為 JSON 檔案
- **?? 載入設定**: 從檔案載入之前儲存的配置
- **自動載入**: 啟動時自動載入上次儲存的設定

### 3. 快速開始提示

右側面板新增黃色提示框，提供四個簡單步驟：
1. 輸入您的 API Key
2. 選擇翻譯目標語言
3. 點擊測試連線
4. 勾選啟用即時翻譯

## 界面更新

### 新的 API Key 區塊

```
┌─────────────────────────────────┐
│ API Key:              ??? ? 顯示 │
├─────────────────────────────────┤
│ [●●●●●●●●●●●●●●●●●●●●●●●●●●●●●] │  ← PasswordBox (預設)
│ [sk-proj-xxxxx...            ] │  ← TextBox (顯示時)
└─────────────────────────────────┘
```

### 快速設定提示框

```
┌─────────────────────────────────┐
│ ?? 快速開始提示                 │
├─────────────────────────────────┤
│ 1. 輸入您的 API Key             │
│ 2. 選擇翻譯目標語言             │
│ 3. 點擊測試連線                 │
│ 4. 勾選啟用即時翻譯             │
└─────────────────────────────────┘
```

### 儲存/載入按鈕

```
┌──────────────┬──────────────┐
│ ?? 儲存設定  │ ?? 載入設定  │
└──────────────┴──────────────┘
```

## 使用方式

### 方法 1: 手動輸入（推薦新使用者）

1. **顯示 API Key**
   - 勾選「??? 顯示」核取方塊
   - API Key 輸入框切換為明文模式

2. **輸入設定**
   ```
   LLM 提供商: OpenAI (GPT)
   API URL: https://api.openai.com/v1/chat/completions
   API Key: sk-proj-xxxxxxxxxxxxx
   翻譯成: 繁體中文
   模型名稱: gpt-3.5-turbo
   ```

3. **儲存設定**
   - 點擊「?? 儲存設定」按鈕
   - 設定會儲存到 `translation_settings.json`
   - 下次啟動自動載入

### 方法 2: 載入設定（推薦老使用者）

1. **準備設定檔案**
   
   在應用程式目錄建立 `translation_settings.json`：

   ```json
   {
     "Provider": "openai",
     "ApiUrl": "https://api.openai.com/v1/chat/completions",
     "ApiKey": "sk-proj-xxxxxxxxxxxxx",
     "ModelName": "gpt-3.5-turbo",
     "TargetLanguage": "zh-TW"
   }
   ```

2. **載入設定**
   - 啟動應用程式
   - 設定會自動載入
   - 或手動點擊「?? 載入設定」

### 方法 3: 環境特定設定

為不同環境準備不同的設定檔案：

**開發環境** (`translation_settings.json`):
```json
{
  "Provider": "ollama",
  "ApiUrl": "http://localhost:11434/api/chat",
  "ApiKey": "",
  "ModelName": "llama2",
  "TargetLanguage": "zh-TW"
}
```

**生產環境** (`translation_settings.prod.json`):
```json
{
  "Provider": "openai",
  "ApiUrl": "https://api.openai.com/v1/chat/completions",
  "ApiKey": "sk-proj-production-key",
  "ModelName": "gpt-4",
  "TargetLanguage": "en"
}
```

## 設定檔案格式

### 完整範例

```json
{
  "Provider": "openai",
  "ApiUrl": "https://api.openai.com/v1/chat/completions",
  "ApiKey": "sk-proj-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "ModelName": "gpt-3.5-turbo",
  "TargetLanguage": "zh-TW"
}
```

### 欄位說明

| 欄位 | 說明 | 可選值 | 範例 |
|------|------|--------|------|
| Provider | LLM 提供商 | openai, azure, anthropic, google, ollama, custom | "openai" |
| ApiUrl | API 端點 | 任何有效的 URL | "https://api.openai.com/v1/chat/completions" |
| ApiKey | API 金鑰 | 提供商的 API Key | "sk-proj-xxxxx..." |
| ModelName | 模型名稱 | 依提供商而定 | "gpt-3.5-turbo" |
| TargetLanguage | 翻譯目標語言 | zh-TW, zh-CN, en, ja, ko, fr, de, es, ru | "zh-TW" |

### 不同提供商的設定範例

#### OpenAI
```json
{
  "Provider": "openai",
  "ApiUrl": "https://api.openai.com/v1/chat/completions",
  "ApiKey": "sk-proj-xxxxx",
  "ModelName": "gpt-3.5-turbo",
  "TargetLanguage": "zh-TW"
}
```

#### Azure OpenAI
```json
{
  "Provider": "azure",
  "ApiUrl": "https://myresource.openai.azure.com/openai/deployments/gpt35/chat/completions?api-version=2024-02-15-preview",
  "ApiKey": "your-azure-api-key",
  "ModelName": "gpt-35-turbo",
  "TargetLanguage": "en"
}
```

#### Ollama (本地)
```json
{
  "Provider": "ollama",
  "ApiUrl": "http://localhost:11434/api/chat",
  "ApiKey": "",
  "ModelName": "llama2",
  "TargetLanguage": "zh-TW"
}
```

## 安全性建議

### 1. API Key 保護

**? 推薦做法：**
- 使用隱藏模式（不勾選「顯示」）
- 定期更換 API Key
- 不要分享包含 API Key 的設定檔案
- 不要將設定檔案提交到版本控制

**? 避免：**
- 在公開場合展示時勾選「顯示」
- 將 API Key 寫在文件中
- 使用相同的 API Key 在多個環境

### 2. 設定檔案權限

**Windows:**
```powershell
# 設定檔案唯讀
attrib +R translation_settings.json

# 或加密檔案
cipher /E translation_settings.json
```

**Linux/macOS:**
```bash
# 限制檔案權限
chmod 600 translation_settings.json

# 只有擁有者可讀寫
chown $USER translation_settings.json
```

### 3. 環境變數（進階）

為更高的安全性，可以使用環境變數：

**設定環境變數：**
```bash
# Windows PowerShell
$env:WHISPER_API_KEY="sk-proj-xxxxx"

# Linux/macOS
export WHISPER_API_KEY="sk-proj-xxxxx"
```

**修改設定檔案使用環境變數：**
```json
{
  "Provider": "openai",
  "ApiUrl": "https://api.openai.com/v1/chat/completions",
  "ApiKey": "${WHISPER_API_KEY}",
  "ModelName": "gpt-3.5-turbo",
  "TargetLanguage": "zh-TW"
}
```

## 技術實作

### API Key 切換機制

```csharp
private void ShowApiKeyCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
{
    var isChecked = ShowApiKeyCheckBox.IsChecked ?? false;
    
    if (isChecked)
    {
        // 顯示模式
        ApiKeyTextBox.Text = ApiKeyPasswordBox.Password;
        ApiKeyPasswordBox.Visibility = Visibility.Collapsed;
        ApiKeyTextBox.Visibility = Visibility.Visible;
    }
    else
    {
        // 隱藏模式
        ApiKeyPasswordBox.Password = ApiKeyTextBox.Text;
        ApiKeyPasswordBox.Visibility = Visibility.Visible;
        ApiKeyTextBox.Visibility = Visibility.Collapsed;
    }
}
```

### 設定儲存

```csharp
private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
{
    var settings = new
    {
        Provider = (LLMProviderComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString(),
        ApiUrl = ApiUrlTextBox.Text,
        ApiKey = GetApiKey(),
        ModelName = ModelNameTextBox.Text,
        TargetLanguage = (TranslationTargetComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString()
    };

    var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
    { 
        WriteIndented = true 
    });

    File.WriteAllText("translation_settings.json", json);
}
```

### 設定載入

```csharp
private void LoadTranslationSettings(bool showMessage = false)
{
    if (!File.Exists("translation_settings.json"))
        return;

    var json = File.ReadAllText("translation_settings.json");
    var settings = JsonSerializer.Deserialize<TranslationSettings>(json);

    // 套用設定到 UI
    ApiUrlTextBox.Text = settings.ApiUrl;
    SetApiKey(settings.ApiKey);
    ModelNameTextBox.Text = settings.ModelName;
    // ... 其他欄位
}
```

## 疑難排解

### Q: 設定檔案儲存在哪裡？
**A**: 儲存在應用程式執行檔案的相同目錄下，檔名為 `translation_settings.json`。

### Q: 可以手動編輯設定檔案嗎？
**A**: 可以！使用任何文字編輯器（如記事本、VS Code）編輯 JSON 檔案。

### Q: 設定檔案遺失怎麼辦？
**A**: 重新輸入設定並點擊「儲存設定」，或手動建立 JSON 檔案。

### Q: API Key 顯示後如何再隱藏？
**A**: 取消勾選「??? 顯示」核取方塊即可。

### Q: 可以匯入/匯出設定到其他電腦嗎？
**A**: 可以！只需複製 `translation_settings.json` 檔案到其他電腦的應用程式目錄。

### Q: 設定檔案格式錯誤怎麼辦？
**A**: 刪除 `translation_settings.json` 並重新儲存設定，或使用 JSON 驗證工具檢查格式。

## 最佳實踐

### 1. 團隊協作

**建立設定範本** (`translation_settings.template.json`):
```json
{
  "Provider": "openai",
  "ApiUrl": "https://api.openai.com/v1/chat/completions",
  "ApiKey": "YOUR_API_KEY_HERE",
  "ModelName": "gpt-3.5-turbo",
  "TargetLanguage": "zh-TW"
}
```

團隊成員複製範本並填入自己的 API Key。

### 2. 版本控制

**.gitignore** 檔案：
```
# 忽略包含敏感資訊的設定
translation_settings.json

# 但保留範本
!translation_settings.template.json
```

### 3. 多環境配置

建立不同環境的設定檔案：
- `translation_settings.dev.json` - 開發環境
- `translation_settings.test.json` - 測試環境  
- `translation_settings.prod.json` - 生產環境

使用時重新命名為 `translation_settings.json`。

### 4. 自動化部署

**PowerShell 腳本** (`setup-translation.ps1`):
```powershell
param (
    [string]$Environment = "dev"
)

$sourceFile = "translation_settings.$Environment.json"
$targetFile = "translation_settings.json"

if (Test-Path $sourceFile) {
    Copy-Item $sourceFile $targetFile
    Write-Host "已載入 $Environment 環境設定"
} else {
    Write-Host "找不到設定檔案: $sourceFile"
}
```

使用：
```powershell
.\setup-translation.ps1 -Environment prod
```

## 更新日誌

### v1.2.2 - API Key 設定與儲存
- ? 新增 API Key 顯示/隱藏切換
- ? 新增設定儲存功能
- ? 新增設定載入功能
- ? 新增自動載入上次設定
- ? 新增快速開始提示框
- ? 優化使用者體驗

## 相關文件

- [LLM翻譯功能說明.md](./LLM翻譯功能說明.md) - 翻譯功能完整說明
- [v1.2.0更新摘要.md](./v1.2.0更新摘要.md) - 翻譯功能介紹
- [快速使用指南.md](./快速使用指南.md) - 應用程式使用指南

---

**版本**: 1.2.2  
**最後更新**: 2024  
**狀態**: ? 已完成
