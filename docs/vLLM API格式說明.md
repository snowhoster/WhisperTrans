# vLLM API 格式說明

## 兩種 API 格式

vLLM 支援兩種 OpenAI 兼容的 API 格式：

### 1. Chat Completions API（推薦）?

**端點**: `/v1/chat/completions`

**適用場景**:
- 對話式模型（Instruct 模型）
- 需要系統提示詞
- 多輪對話
- 角色扮演

**請求格式**:
```json
{
  "model": "Qwen/Qwen2-7B-Instruct",
  "messages": [
    {
      "role": "system",
      "content": "你是一個專業的翻譯助手"
    },
    {
      "role": "user",
      "content": "請將以下文字翻譯成繁體中文：Hello"
    }
  ],
  "temperature": 0.3,
  "max_tokens": 1000
}
```

**回應格式**:
```json
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "你好"
      }
    }
  ]
}
```

**curl 範例**:
```bash
curl http://localhost:8000/v1/chat/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "Qwen/Qwen2-7B-Instruct",
    "messages": [
      {"role": "user", "content": "Hello"}
    ]
  }'
```

### 2. Completions API（舊版）

**端點**: `/v1/completions`

**適用場景**:
- 基礎模型（非 Instruct）
- 簡單的文本生成
- 不需要對話上下文
- 向後兼容

**請求格式**:
```json
{
  "model": "Qwen/Qwen3-VL-30B-A3B-Instruct-FP8",
  "prompt": "你好，請介紹一下什麼是 vLLM",
  "max_tokens": 50,
  "temperature": 0
}
```

**回應格式**:
```json
{
  "choices": [
    {
      "text": "vLLM 是一個高效能的大語言模型推理引擎..."
    }
  ]
}
```

**curl 範例**:
```bash
curl http://192.168.100.10:8000/v1/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "Qwen/Qwen3-VL-30B-A3B-Instruct-FP8",
    "prompt": "你好，請介紹一下什麼是 vLLM",
    "max_tokens": 50,
    "temperature": 0
  }'
```

## WhisperTrans 設定

### 方式 1: Chat Completions（推薦）

```
LLM 提供商: 本地模型 (vLLM)
API URL: http://localhost:8000/v1/chat/completions
API Key: (留空)
模型名稱: Qwen/Qwen2-7B-Instruct
```

**適合模型**:
- ? Qwen2-7B-Instruct
- ? Llama-3.1-8B-Instruct
- ? Mistral-7B-Instruct
- ? Yi-1.5-9B-Chat

### 方式 2: Completions（舊版）

```
LLM 提供商: 本地模型 (vLLM)
API URL: http://localhost:8000/v1/completions
API Key: (留空)
模型名稱: Qwen/Qwen3-VL-30B-A3B-Instruct-FP8
```

**適合模型**:
- ?? 基礎模型
- ?? 某些特殊格式的模型

## 您的配置範例

根據您提供的 curl 命令：

```bash
curl http://192.168.100.10:8000/v1/completions \
  -H "Content-Type: application/json" \
  -d '{
    "model": "Qwen/Qwen3-VL-30B-A3B-Instruct-FP8",
    "prompt": "你好，請介紹一下什麼是 vLLM",
    "max_tokens": 50,
    "temperature": 0
  }'
```

**WhisperTrans 設定**:
```
LLM 提供商: 本地模型 (vLLM)
API URL: http://192.168.100.10:8000/v1/completions
API Key: (留空)
模型名稱: Qwen/Qwen3-VL-30B-A3B-Instruct-FP8
翻譯成: 繁體中文
```

## 如何在 WhisperTrans 中設定

1. **選擇提供商**
   - LLM 提供商 → 本地模型 (vLLM)
   - 會彈出對話框詢問 API 格式

2. **選擇 API 格式**
   - 如果使用 `/v1/completions`，點擊「是」
   - 如果使用 `/v1/chat/completions`，點擊「否」

3. **完成設定**
   - 修改 IP 地址（如果需要）
   - 輸入模型名稱: `Qwen/Qwen3-VL-30B-A3B-Instruct-FP8`
   - 測試連線

## 區別對比

| 項目 | Chat Completions | Completions |
|------|------------------|-------------|
| **端點** | `/v1/chat/completions` | `/v1/completions` |
| **請求格式** | `messages` 陣列 | 單一 `prompt` |
| **系統提示** | ? 支援 | ? 不支援 |
| **回應欄位** | `message.content` | `text` |
| **推薦度** | ????? | ??? |

## 自動檢測

WhisperTrans 會自動檢測您使用的 API 格式：

```
/v1/chat/completions  → 使用 Chat Completions 格式
/v1/completions       → 使用 Completions 格式
```

不需要額外設定，只需輸入正確的 URL 即可！

---

**版本**: 1.2.5  
**最後更新**: 2024  
**狀態**: ? 支援雙格式
