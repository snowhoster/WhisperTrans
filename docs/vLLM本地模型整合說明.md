# vLLM 本地模型整合說明

## 概述

WhisperTrans 現在支援使用 vLLM 作為本地大語言模型翻譯引擎。vLLM 是一個高效能的 LLM 推理和服務引擎，可以在本地運行各種開源大語言模型。

## 什麼是 vLLM？

**vLLM** (Very Large Language Model) 是一個快速且易於使用的 LLM 推理和服務庫，具有以下特點：

- ?? **高效能推理** - 使用 PagedAttention 優化記憶體使用
- ?? **易於部署** - 兼容 OpenAI API 格式
- ?? **本地運行** - 完全在本地執行，資料不外傳
- ?? **完全免費** - 開源且免費使用
- ?? **支援多種模型** - Llama、Mistral、Qwen、Yi 等

## 主要優勢

### vs OpenAI API
- ? **完全免費** - 無需付費
- ? **資料隱私** - 資料不離開本機
- ? **無限制使用** - 沒有配額限制
- ? **需要硬體** - 需要足夠的 GPU/CPU 資源

### vs Ollama
- ? **更快的推理速度** - PagedAttention 優化
- ? **更好的批次處理** - 支援並發請求
- ? **OpenAI 兼容 API** - 易於整合
- ? **設定較複雜** - 需要手動下載模型

## 系統需求

### 最低需求
- **作業系統**: Windows 10/11, Linux, macOS
- **RAM**: 8GB+
- **儲存空間**: 10GB+（依模型大小）
- **Python**: 3.8+

### 推薦配置
- **GPU**: NVIDIA GPU with 8GB+ VRAM (RTX 3060 或更高)
- **RAM**: 16GB+
- **CUDA**: 11.8+ (for NVIDIA GPU)

### 支援的硬體
- **NVIDIA GPU** - 最佳效能（推薦）
- **AMD GPU** - 透過 ROCm（實驗性）
- **CPU** - 較慢但可用

## 安裝 vLLM

### Windows (NVIDIA GPU)

1. **安裝 Python**
```powershell
# 從 python.org 下載並安裝 Python 3.8+
# 確認安裝
python --version
```

2. **安裝 CUDA Toolkit**
```powershell
# 從 NVIDIA 官網下載並安裝 CUDA 11.8+
# https://developer.nvidia.com/cuda-downloads
```

3. **安裝 vLLM**
```powershell
# 使用 pip 安裝
pip install vllm

# 或使用 conda
conda install -c conda-forge vllm
```

### Linux (NVIDIA GPU)

```bash
# 安裝 Python 和 CUDA
sudo apt update
sudo apt install python3 python3-pip

# 安裝 vLLM
pip install vllm
```

### macOS (Apple Silicon)

```bash
# vLLM 目前對 macOS 支援有限
# 建議使用 Ollama 或 Docker
brew install python
pip install vllm
```

## 下載模型

### 推薦的中文翻譯模型

#### 1. Qwen2-7B-Instruct (推薦)
```bash
# 使用 Hugging Face CLI
huggingface-cli download Qwen/Qwen2-7B-Instruct

# 或使用 Python
python -c "from huggingface_hub import snapshot_download; snapshot_download('Qwen/Qwen2-7B-Instruct')"
```

**特點：**
- 大小: ~14GB
- 記憶體需求: 16GB RAM 或 8GB VRAM
- 中文能力: ?????
- 速度: 快

#### 2. Llama-3.1-8B-Instruct
```bash
huggingface-cli download meta-llama/Meta-Llama-3.1-8B-Instruct
```

**特點：**
- 大小: ~16GB
- 記憶體需求: 16GB RAM 或 10GB VRAM
- 翻譯品質: ?????
- 多語言支援: 優秀

#### 3. Yi-1.5-9B-Chat
```bash
huggingface-cli download 01-ai/Yi-1.5-9B-Chat
```

**特點：**
- 大小: ~18GB
- 中文能力: ?????
- 由零一萬物開發，專長中文

#### 4. Mistral-7B-Instruct (英文優先)
```bash
huggingface-cli download mistralai/Mistral-7B-Instruct-v0.3
```

**特點：**
- 大小: ~14GB
- 速度: 非常快
- 適合英文翻譯

## 啟動 vLLM 伺服器

### 基本啟動

#### 本地單機使用 (localhost)
```bash
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --host 127.0.0.1 \
    --port 8000
```

**URL**: `http://localhost:8000/v1/chat/completions`

#### 區網共享使用
```bash
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --host 0.0.0.0 \
    --port 8000
```

**URL**: `http://192.168.100.10:8000/v1/chat/completions`  
（替換為您的實際 IP）

### 進階參數

#### GPU 加速
```bash
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --gpu-memory-utilization 0.9 \
    --max-model-len 4096
```

#### CPU 運行（無 GPU）
```bash
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --device cpu \
    --dtype float32
```

#### 量化模型（節省記憶體）
```bash
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --quantization awq \
    --dtype half
```

### Windows 啟動腳本範例

建立 `start_vllm.bat`：
```batch
@echo off
echo 正在啟動 vLLM 伺服器...
echo.

python -m vllm.entrypoints.openai.api_server ^
    --model Qwen/Qwen2-7B-Instruct ^
    --host 127.0.0.1 ^
    --port 8000 ^
    --gpu-memory-utilization 0.9

pause
```

## WhisperTrans 設定

### 1. 本地運行 (localhost)

在 WhisperTrans 中設定：

```
LLM 提供商: 本地模型 (vLLM)
API URL: http://localhost:8000/v1/chat/completions
API Key: (留空或任意文字)
翻譯成: 繁體中文
模型名稱: Qwen/Qwen2-7B-Instruct
```

### 2. 區網運行

如果 vLLM 在區網另一台電腦：

```
LLM 提供商: 本地模型 (vLLM)
API URL: http://192.168.100.10:8000/v1/chat/completions
API Key: (留空)
翻譯成: 繁體中文
模型名稱: Qwen/Qwen2-7B-Instruct
```

### 3. 驗證連線

1. 啟動 vLLM 伺服器
2. 在 WhisperTrans 中點擊「?? 測試連線」
3. 如果成功，會顯示「? 連線成功！」

## 使用範例

### 完整工作流程

#### 步驟 1: 準備模型
```bash
# 下載模型
huggingface-cli download Qwen/Qwen2-7B-Instruct
```

#### 步驟 2: 啟動 vLLM
```bash
# 在終端機執行
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --host 127.0.0.1 \
    --port 8000
```

您會看到類似輸出：
```
INFO:     Started server process [12345]
INFO:     Waiting for application startup.
INFO:     Application startup complete.
INFO:     Uvicorn running on http://127.0.0.1:8000
```

#### 步驟 3: 設定 WhisperTrans
1. 開啟 WhisperTrans
2. 在右側翻譯面板：
   - LLM 提供商 → 選擇「本地模型 (vLLM)」
   - API URL → 自動填入 `http://localhost:8000/v1/chat/completions`
   - API Key → 留空
   - 翻譯成 → 繁體中文
   - 模型名稱 → Qwen/Qwen2-7B-Instruct
3. 點擊「測試連線」確認

#### 步驟 4: 開始翻譯
1. 勾選「? 啟用即時翻譯」
2. 初始化 Whisper 模型
3. 開始錄音
4. 享受即時語音轉文字與翻譯！

### 翻譯範例

**輸入（英文）:**
```
Hello, how are you today?
```

**輸出（繁體中文）:**
```
你好，你今天好嗎？
```

**輸入（繁體中文）:**
```
今天天氣很好，我們去公園走走吧。
```

**輸出（英文）:**
```
The weather is nice today, let's go for a walk in the park.
```

## 效能優化

### 1. GPU 記憶體優化

```bash
# 調整 GPU 記憶體使用率
--gpu-memory-utilization 0.8  # 使用 80% GPU 記憶體
```

### 2. 批次大小優化

```bash
# 增加批次大小以提高吞吐量
--max-num-batched-tokens 8192
--max-num-seqs 256
```

### 3. 量化優化

使用量化模型可以顯著降低記憶體需求：

```bash
# AWQ 4-bit 量化
--quantization awq

# GPTQ 4-bit 量化
--quantization gptq
```

### 4. 上下文長度優化

```bash
# 限制最大長度以節省記憶體
--max-model-len 2048
```

## 疑難排解

### Q: 啟動 vLLM 時顯示 CUDA out of memory
**A**: 
```bash
# 解決方法 1: 降低 GPU 記憶體使用率
--gpu-memory-utilization 0.7

# 解決方法 2: 使用量化模型
--quantization awq

# 解決方法 3: 減少最大長度
--max-model-len 2048

# 解決方法 4: 使用更小的模型
# 改用 Qwen2-1.5B 或 Qwen2-0.5B
```

### Q: 翻譯速度很慢
**A**:
1. 確認使用 GPU 而非 CPU
2. 檢查 GPU 記憶體是否充足
3. 嘗試使用更小但更快的模型
4. 調整批次大小參數

### Q: 無法連線到 vLLM
**A**:
1. 確認 vLLM 伺服器已啟動
2. 檢查防火牆設定
3. 確認 URL 正確（localhost vs IP）
4. 檢查端口是否被占用

### Q: 翻譯品質不好
**A**:
1. 嘗試使用更大的模型（如 Qwen2-7B → Qwen2-14B）
2. 檢查模型是否專長該語言
3. 調整溫度參數（在代碼中）
4. 使用 Instruct 版本的模型

### Q: 模型下載失敗
**A**:
```bash
# 使用鏡像站點
HF_ENDPOINT=https://hf-mirror.com huggingface-cli download Qwen/Qwen2-7B-Instruct

# 或手動下載
# 1. 訪問 https://huggingface.co/Qwen/Qwen2-7B-Instruct
# 2. 點擊 Files and versions
# 3. 手動下載所有檔案
```

## 效能對比

### 翻譯速度對比

| 模型 | 硬體 | 速度 (tokens/s) | 延遲 (ms) |
|------|------|----------------|----------|
| Qwen2-7B | RTX 4090 | ~120 | 300-500 |
| Qwen2-7B | RTX 3060 | ~60 | 600-1000 |
| Llama-3.1-8B | RTX 4090 | ~100 | 400-600 |
| Mistral-7B | RTX 3060 | ~80 | 400-700 |
| Qwen2-7B | CPU (i9) | ~10 | 3000-5000 |

### 記憶體需求對比

| 模型 | FP16 | AWQ 4-bit | 推薦硬體 |
|------|------|-----------|---------|
| Qwen2-0.5B | 1GB | 0.5GB | 任何 GPU |
| Qwen2-1.5B | 3GB | 1.5GB | GTX 1660+ |
| Qwen2-7B | 14GB | 7GB | RTX 3060+ |
| Llama-3.1-8B | 16GB | 8GB | RTX 3070+ |
| Qwen2-14B | 28GB | 14GB | RTX 4090 |

## 進階設定

### Docker 部署

```dockerfile
# Dockerfile
FROM vllm/vllm-openai:latest

# 下載模型
RUN pip install huggingface_hub
RUN python -c "from huggingface_hub import snapshot_download; snapshot_download('Qwen/Qwen2-7B-Instruct')"

# 啟動服務
CMD ["--model", "Qwen/Qwen2-7B-Instruct", "--host", "0.0.0.0", "--port", "8000"]
```

啟動：
```bash
docker build -t my-vllm .
docker run -p 8000:8000 --gpus all my-vllm
```

### 多 GPU 支援

```bash
# 使用多個 GPU
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --tensor-parallel-size 2  # 使用 2 個 GPU
```

### API Key 保護（選配）

```bash
# 啟用 API Key 驗證
python -m vllm.entrypoints.openai.api_server \
    --model Qwen/Qwen2-7B-Instruct \
    --api-key your-secret-key
```

在 WhisperTrans 中：
```
API Key: your-secret-key
```

## 成本效益分析

### vs 雲端 API

**使用 OpenAI API (GPT-3.5-turbo):**
- 每月成本（中度使用）: ~$200
- 資料隱私: ?
- 速度: 快
- 品質: 優秀

**使用 vLLM 本地部署:**
- 初始投資: $500-2000（GPU）
- 電費（每月）: ~$20-50
- 資料隱私: ?
- 速度: 非常快（本地）
- 品質: 優秀

**回本時間:** 約 3-6 個月

## 推薦配置

### 個人使用
```
模型: Qwen2-7B-Instruct
硬體: RTX 3060 12GB
記憶體: 16GB RAM
估計成本: ~$1000
```

### 小團隊使用
```
模型: Qwen2-14B-Instruct
硬體: RTX 4090 24GB
記憶體: 32GB RAM
估計成本: ~$2500
```

### 企業使用
```
模型: Qwen2-72B-Instruct
硬體: 2x A100 80GB
記憶體: 128GB RAM
估計成本: ~$20000
```

## 相關資源

- [vLLM 官方文件](https://docs.vllm.ai/)
- [vLLM GitHub](https://github.com/vllm-project/vllm)
- [Qwen 模型](https://huggingface.co/Qwen)
- [Llama 模型](https://huggingface.co/meta-llama)
- [Hugging Face 模型庫](https://huggingface.co/models)

## 總結

vLLM 是一個強大的本地 LLM 解決方案，特別適合：

? **需要資料隱私的使用者**  
? **頻繁使用翻譯功能的使用者**  
? **有 GPU 硬體的使用者**  
? **想要降低長期成本的使用者**  

透過 WhisperTrans 整合 vLLM，您可以享受完全本地化、高品質、且無成本限制的即時語音翻譯服務！

---

**版本**: 1.2.3  
**最後更新**: 2024  
**狀態**: ? 已完成
