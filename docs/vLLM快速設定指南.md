# vLLM 快速設定指南

## ?? 5 分鐘快速啟動

### 步驟 1: 安裝 vLLM (2 分鐘)

```bash
# Windows / Linux
pip install vllm
```

### 步驟 2: 下載模型 (視網速而定)

**推薦模型：Qwen2-7B-Instruct**

```bash
# 使用 Hugging Face CLI (需先安裝: pip install huggingface_hub)
huggingface-cli download Qwen/Qwen2-7B-Instruct
```

**國內用戶加速：**
```bash
# 使用鏡像站點
HF_ENDPOINT=https://hf-mirror.com huggingface-cli download Qwen/Qwen2-7B-Instruct
```

### 步驟 3: 啟動 vLLM 伺服器 (1 分鐘)

**本機使用：**
```bash
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --host 127.0.0.1 \
    --port 8000
```

**區網共享：**
```bash
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --host 0.0.0.0 \
    --port 8000
```

等待看到：
```
INFO:     Uvicorn running on http://127.0.0.1:8000
```

### 步驟 4: 在 WhisperTrans 中設定 (1 分鐘)

1. 開啟 WhisperTrans
2. 在右側「?? 大語言模型翻譯」區域：

```
┌─────────────────────────────────────┐
│ LLM 提供商: [本地模型 (vLLM) ▼]    │
│                                     │
│ API URL:                            │
│ http://localhost:8000/v1/chat/      │
│ completions                         │
│                                     │
│ API Key: (留空即可)                 │
│                                     │
│ 翻譯成: [繁體中文 ▼]                │
│                                     │
│ 模型名稱:                           │
│ Qwen/Qwen2-7B-Instruct              │
└─────────────────────────────────────┘
```

3. 點擊「?? 測試連線」
4. 看到「? 連線成功！」
5. 勾選「? 啟用即時翻譯」

### 步驟 5: 開始使用！

? 完成！現在您可以開始使用本地翻譯了！

---

## ?? 完整配置表

### 本地使用 (localhost)

| 設定項目 | 值 |
|---------|---|
| LLM 提供商 | 本地模型 (vLLM) |
| API URL | `http://localhost:8000/v1/chat/completions` |
| API Key | (留空) |
| 翻譯成 | 繁體中文 |
| 模型名稱 | Qwen/Qwen2-7B-Instruct |

### 區網使用

| 設定項目 | 值 |
|---------|---|
| LLM 提供商 | 本地模型 (vLLM) |
| API URL | `http://192.168.100.10:8000/v1/chat/completions` |
| API Key | (留空) |
| 翻譯成 | 繁體中文 |
| 模型名稱 | Qwen/Qwen2-7B-Instruct |

> **注意**: 將 `192.168.100.10` 替換為您的實際 IP 位址

---

## ?? 常用模型與設定

### 輕量級（記憶體有限）

**Qwen2-1.5B**
```bash
# 啟動命令
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-1.5B-Instruct \
    --host 127.0.0.1 \
    --port 8000

# WhisperTrans 設定
模型名稱: Qwen/Qwen2-1.5B-Instruct
```
- 記憶體需求: ~4GB
- 適合: GTX 1650 或更低階 GPU

### 推薦配置（平衡）

**Qwen2-7B** (推薦)
```bash
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --host 127.0.0.1 \
    --port 8000

# WhisperTrans 設定
模型名稱: Qwen/Qwen2-7B-Instruct
```
- 記憶體需求: ~14GB
- 適合: RTX 3060 12GB 或更高

### 高品質（效能優先）

**Qwen2-14B**
```bash
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-14B-Instruct \
    --host 127.0.0.1 \
    --port 8000

# WhisperTrans 設定
模型名稱: Qwen/Qwen2-14B-Instruct
```
- 記憶體需求: ~28GB
- 適合: RTX 4090 24GB 或 A6000

---

## ?? 常見問題速查

### Q: 如何知道 vLLM 已經啟動成功？
**A**: 終端機會顯示：
```
INFO:     Uvicorn running on http://127.0.0.1:8000
```

### Q: 如何獲取我的 IP 位址？
**A**:
```bash
# Windows
ipconfig

# Linux/Mac
ifconfig
# 或
ip addr show
```
找到 `192.168.x.x` 格式的 IP

### Q: API Key 一定要留空嗎？
**A**: 預設情況下，vLLM 不需要 API Key。如果您在啟動時設定了 `--api-key`，則需要填入相同的 key。

### Q: 可以同時連接多個客戶端嗎？
**A**: 可以！vLLM 支援並發請求。

### Q: 翻譯品質如何？
**A**: 
- Qwen2-7B: 接近 GPT-3.5 水準
- Qwen2-14B: 接近 GPT-4 水準
- 特別是中文翻譯，表現優異

---

## ?? 快速啟動腳本

### Windows (start_vllm.bat)

```batch
@echo off
echo ========================================
echo    WhisperTrans vLLM 伺服器啟動工具
echo ========================================
echo.

echo 正在啟動 Qwen2-7B 模型...
echo 請稍候，這可能需要 1-2 分鐘...
echo.

python -m vllm.entrypoints.openai.api_server ^
    --model Qwen/Qwen2-7B-Instruct ^
    --host 127.0.0.1 ^
    --port 8000 ^
    --gpu-memory-utilization 0.9

pause
```

### Linux/macOS (start_vllm.sh)

```bash
#!/bin/bash

echo "========================================"
echo "   WhisperTrans vLLM 伺服器啟動工具"
echo "========================================"
echo ""

echo "正在啟動 Qwen2-7B 模型..."
echo "請稍候，這可能需要 1-2 分鐘..."
echo ""

python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --host 127.0.0.1 \
    --port 8000 \
    --gpu-memory-utilization 0.9
```

使用：
```bash
# 賦予執行權限
chmod +x start_vllm.sh

# 執行
./start_vllm.sh
```

---

## ?? 疑難排解速查

### 問題：CUDA out of memory

**解決方法：**
```bash
# 方法 1: 降低 GPU 使用率
--gpu-memory-utilization 0.7

# 方法 2: 使用更小的模型
--model Qwen/Qwen2-1.5B-Instruct

# 方法 3: 限制最大長度
--max-model-len 2048
```

### 問題：連線失敗

**檢查清單：**
1. ? vLLM 伺服器是否已啟動？
2. ? URL 是否正確？(`localhost` vs IP)
3. ? 端口 8000 是否被占用？
4. ? 防火牆是否阻擋？

### 問題：翻譯速度慢

**優化方法：**
1. 確認使用 GPU 加速
2. 關閉其他佔用 GPU 的程式
3. 使用更小但更快的模型
4. 調整 `--gpu-memory-utilization`

---

## ?? 效能參考

### 我的硬體能跑嗎？

| GPU | 推薦模型 | 預期速度 |
|-----|---------|---------|
| RTX 4090 | Qwen2-14B | 非常快 ??? |
| RTX 4080 | Qwen2-7B | 非常快 ??? |
| RTX 3090 | Qwen2-7B | 快 ?? |
| RTX 3060 12GB | Qwen2-7B | 中等 ? |
| RTX 3060 8GB | Qwen2-1.5B | 中等 ? |
| GTX 1660 | Qwen2-1.5B | 慢 |
| CPU Only | Qwen2-1.5B | 非常慢 |

---

## ?? 學習資源

- ?? [完整 vLLM 說明](./vLLM本地模型整合說明.md)
- ?? [LLM 翻譯功能說明](./LLM翻譯功能說明.md)
- ?? [快速使用指南](./快速使用指南.md)

---

**最後更新**: 2024  
**版本**: 1.2.3  
**狀態**: ? 可用
