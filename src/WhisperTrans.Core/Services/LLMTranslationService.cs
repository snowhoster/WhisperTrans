using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhisperTrans.Core.Services;

/// <summary>
/// 大語言模型翻譯服務
/// </summary>
public class LLMTranslationService : IDisposable
{
    private readonly HttpClient _httpClient;
    private string _apiUrl = string.Empty;
    private string _apiKey = string.Empty;
    private string _modelName = "gpt-3.5-turbo";
    private string _targetLanguage = "zh-TW";
    private bool _disposed;
    private bool _useLegacyCompletionsApi = false;

    public LLMTranslationService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// 配置翻譯服務
    /// </summary>
    public void Configure(string apiUrl, string apiKey, string modelName, string targetLanguage)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _modelName = modelName;
        _targetLanguage = targetLanguage;

        // 判斷是否使用舊版 Completions API
        _useLegacyCompletionsApi = apiUrl.Contains("/completions") && !apiUrl.Contains("/chat/completions");

        // 只有在有 API Key 的情況下才設定 Authorization header
        // 本地模型如 vLLM 和 Ollama 通常不需要
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _apiKey);
        }
        else
        {
            // 移除 Authorization header（如果之前有設定過）
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    /// <summary>
    /// 翻譯文字
    /// </summary>
    public async Task<string> TranslateAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // 只檢查 API URL，API Key 是選填的（本地模型不需要）
        if (string.IsNullOrWhiteSpace(_apiUrl))
            throw new InvalidOperationException("翻譯服務尚未配置 API URL");

        try
        {
            var languageName = GetLanguageName(_targetLanguage);
            var languageCode = GetLanguageCode(_targetLanguage);
            
            object requestBody;
            
            if (_useLegacyCompletionsApi)
            {
                // 使用舊版 Completions API 格式
                var prompt = BuildTranslationPrompt(text, languageName, languageCode, false);
                
                requestBody = new
                {
                    model = _modelName,
                    prompt = prompt,
                    temperature = 0.1,  // 降低創造性，提高準確性
                    max_tokens = 500,   // 減少 tokens 避免冗長回應
                    top_p = 0.9,
                    frequency_penalty = 0.0,
                    presence_penalty = 0.0
                };
            }
            else
            {
                // 使用標準 Chat Completions API 格式
                var systemPrompt = BuildSystemPrompt(languageName, languageCode);
                var userPrompt = BuildUserPrompt(text);
                
                requestBody = new
                {
                    model = _modelName,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    temperature = 0.1,  // 降低創造性，提高準確性
                    max_tokens = 500,   // 減少 tokens 避免冗長回應
                    top_p = 0.9,
                    frequency_penalty = 0.0,
                    presence_penalty = 0.0
                };
            }

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            System.Diagnostics.Debug.WriteLine($"API 請求 URL: {_apiUrl}");
            System.Diagnostics.Debug.WriteLine($"API 格式: {(_useLegacyCompletionsApi ? "Completions" : "Chat Completions")}");
            System.Diagnostics.Debug.WriteLine($"請求內容: {jsonContent}");

            var response = await _httpClient.PostAsync(_apiUrl, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            
            System.Diagnostics.Debug.WriteLine($"回應狀態: {response.StatusCode}");
            System.Diagnostics.Debug.WriteLine($"回應內容: {responseContent}");
            
            response.EnsureSuccessStatusCode();

            // 根據 API 類型解析回應
            string rawTranslation;
            if (_useLegacyCompletionsApi)
            {
                var result = JsonSerializer.Deserialize<CompletionsResponse>(responseContent);
                rawTranslation = result?.Choices?.FirstOrDefault()?.Text?.Trim() ?? string.Empty;
            }
            else
            {
                var result = JsonSerializer.Deserialize<ChatCompletionsResponse>(responseContent);
                rawTranslation = result?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
            }

            // 清理翻譯結果，移除不必要的說明文字
            return CleanTranslationResult(rawTranslation);
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine($"HTTP 請求異常: {ex}");
            throw new Exception($"翻譯請求失敗: {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"JSON 解析異常: {ex}");
            throw new Exception($"解析翻譯結果失敗: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 建立 System Prompt（用於 Chat Completions API）
    /// </summary>
    private string BuildSystemPrompt(string languageName, string languageCode)
    {
        return $@"你是一個專業的即時語音翻譯助手。你的任務是將語音轉錄文字精確翻譯成{languageName}。

重要規則：
1. 只輸出翻譯結果，不要包含任何解釋、說明或額外文字
2. 保持原文的語氣和風格（正式/非正式、口語/書面）
3. 保留原文的標點符號結構
4. 如果原文包含專有名詞（人名、地名、品牌名），保持原文或音譯
5. 對於口語化表達，使用自然的{languageName}口語翻譯
6. 翻譯要簡潔、準確、符合{languageName}的語言習慣
7. 不要添加「翻譯：」、「結果：」等前綴
8. 不要解釋為什麼這樣翻譯

範例：
輸入：""Hello, how are you doing today?""
輸出：""你好，你今天過得怎麼樣？""

輸入：""I'll send you the report by tomorrow.""
輸出：""我明天之前會把報告發給你。""

輸入：""The meeting is scheduled for 3 PM.""
輸出：""會議安排在下午3點。""";
    }

    /// <summary>
    /// 建立 User Prompt（用於 Chat Completions API）
    /// </summary>
    private string BuildUserPrompt(string text)
    {
        return text;  // 直接使用原文，不添加額外說明
    }

    /// <summary>
    /// 建立翻譯 Prompt（用於 Completions API）
    /// </summary>
    private string BuildTranslationPrompt(string text, string languageName, string languageCode, bool includeExamples)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine($"任務：將以下文字精確翻譯成{languageName}");
        prompt.AppendLine();
        prompt.AppendLine("規則：");
        prompt.AppendLine("- 只輸出翻譯結果");
        prompt.AppendLine("- 保持原文語氣和風格");
        prompt.AppendLine("- 專有名詞保持原文或音譯");
        prompt.AppendLine($"- 使用自然的{languageName}表達");
        prompt.AppendLine("- 不要添加任何解釋或說明");
        prompt.AppendLine();
        prompt.AppendLine("原文：");
        prompt.AppendLine(text);
        prompt.AppendLine();
        prompt.Append($"{languageName}翻譯：");
        
        return prompt.ToString();
    }

    /// <summary>
    /// 清理翻譯結果
    /// </summary>
    private string CleanTranslationResult(string translation)
    {
        if (string.IsNullOrWhiteSpace(translation))
            return string.Empty;

        // 移除常見的前綴
        var prefixesToRemove = new[]
        {
            "翻譯：", "翻?：", "Translation:", "譯文：", "?文：",
            "結果：", "?果：", "Result:", "答案：", "答案:",
            "以下是翻譯：", "以下是翻?：", "翻譯結果：", "翻??果：",
            "繁體中文：", "繁体中文：", "簡體中文：", "?体中文：",
            "英文：", "英文:", "中文：", "中文:",
            "Output:", "?出：", "輸出："
        };

        var cleaned = translation.Trim();
        
        foreach (var prefix in prefixesToRemove)
        {
            if (cleaned.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(prefix.Length).Trim();
            }
        }

        // 移除引號（如果整個翻譯被引號包圍）
        if ((cleaned.StartsWith("\"") && cleaned.EndsWith("\"")) ||
            (cleaned.StartsWith("「") && cleaned.EndsWith("」")) ||
            (cleaned.StartsWith(""") && cleaned.EndsWith(""")))
        {
            cleaned = cleaned.Substring(1, cleaned.Length - 2).Trim();
        }

        // 移除換行符號和多餘空白
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned;
    }

    /// <summary>
    /// 取得語言代碼
    /// </summary>
    private string GetLanguageCode(string languageCode)
    {
        return languageCode switch
        {
            "zh-TW" => "zh-Hant",
            "zh-CN" => "zh-Hans",
            "en" => "en",
            "ja" => "ja",
            "ko" => "ko",
            "fr" => "fr",
            "de" => "de",
            "es" => "es",
            "ru" => "ru",
            _ => "zh-Hant"
        };
    }

    /// <summary>
    /// 取得語言名稱
    /// </summary>
    private string GetLanguageName(string languageCode)
    {
        return languageCode switch
        {
            "zh-TW" => "繁體中文",
            "zh-CN" => "簡體中文",
            "en" => "英文",
            "ja" => "日文",
            "ko" => "韓文",
            "fr" => "法文",
            "de" => "德文",
            "es" => "西班牙文",
            "ru" => "俄文",
            _ => "繁體中文"
        };
    }

    /// <summary>
    /// 測試 API 連線
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 檢查基本配置
            if (string.IsNullOrWhiteSpace(_apiUrl))
                return false;

            var testText = "Hello";
            var result = await TranslateAsync(testText, cancellationToken);
            return !string.IsNullOrWhiteSpace(result);
        }
        catch (Exception ex)
        {
            // 記錄錯誤以便調試
            System.Diagnostics.Debug.WriteLine($"連線測試失敗: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _httpClient?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    // Chat Completions API 回應模型（標準格式）
    private class ChatCompletionsResponse
    {
        [JsonPropertyName("choices")]
        public List<ChatChoice>? Choices { get; set; }
    }

    private class ChatChoice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    // Completions API 回應模型（舊版格式）
    private class CompletionsResponse
    {
        [JsonPropertyName("choices")]
        public List<CompletionChoice>? Choices { get; set; }
    }

    private class CompletionChoice
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
