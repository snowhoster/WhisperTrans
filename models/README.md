# models 目錄

此目錄用於存放 Whisper 模型檔案。

## 下載模型

請前往以下連結下載 Whisper 模型：

https://huggingface.co/ggerganov/whisper.cpp/tree/main

## 推薦模型

- **ggml-tiny.bin** (75 MB) - 最快，適合測試
- **ggml-base.bin** (140 MB) - 推薦日常使用
- **ggml-small.bin** (460 MB) - 更高準確度
- **ggml-medium.bin** (1.5 GB) - 專業級
- **ggml-large-v3.bin** (3 GB) - 最高準確度

## 使用方式

下載模型後，將檔案放置於此目錄下，例如：

```
models/
├── ggml-base.bin
├── ggml-small.bin
└── ggml-medium.bin
```

然後在應用程式中指定模型路徑：

```csharp
var config = new WhisperConfig
{
    ModelPath = "models/ggml-base.bin",
    // ...
};
```
