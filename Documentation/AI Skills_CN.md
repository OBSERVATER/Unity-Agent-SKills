# AI Skills (`com.observater.aiskills`)

**AI Skills** 是一个 Unity 编辑器扩展，旨在将大语言模型 (LLM) 直接集成到您的工作流中。它通过桥接 Unity 的 C# 环境与本地 Python 执行引擎，允许您使用自然语言指令控制 Unity 编辑器。

## 功能特性

* **自然语言控制**：只需通过描述即可创建 GameObject、材质、灯光和 UI 元素。
* **Python.NET 集成**：直接在 Unity 内部执行 AI 生成的 Python 代码，可完整访问 `UnityEngine` 和 `UnityEditor` API。
* **技能架构 (Skills)**：模块化的 `.md` 技能文件定义了 AI 的能力（场景、资源、组件管理），使 AI 具备上下文感知能力并易于扩展。
* **上下文感知**：支持在提示词中附加项目文件或文件夹，让 AI 了解您的脚本或资源上下文。
* **可定制后端**：可配置 API 端点（默认 DeepSeek）、模型选择和执行端口。

---

## 安装说明

### 前置要求
1.  **Unity 版本**：2021.3 或更高版本（推荐）。
2.  **网络连接**：需要连接互联网以访问 LLM API（如 DeepSeek, OpenAI）。

## 快速开始

### 1. 打开 Copilot 窗口
在 Unity 菜单栏中选择：
**Tools > AI Copilot**

### 2. 配置服务
在 Copilot 窗口中，展开 **Settings** 折叠栏：
* **API Key**：输入您的 LLM 提供商的 API Key（例如 `sk-xxxx`）。
* **Base URL**：默认为 `https://api.deepseek.com`。如使用 OpenAI 或本地 LLM 请修改此项。
* **Model**：默认为 `deepseek-coder`。
* **Python Port**：默认 `5000` (Server) / `8081` (Unity Execution)。

### 3. 启动服务
点击工具栏中的 **Restart Service** 按钮。
* 这将启动后台 Python 服务器 (`ai_server.py`)。
* 观察窗口底部的 **Process Log**，等待出现 `[System] Unity Socket Server Started`。

---

## 使用指南

### 基本指令
在聊天输入框中输入您的需求并按 **Send**（或回车）。

* *“创建一个名为 'Player' 的红色立方体，并给它添加 Rigidbody。”*
* *“查找项目中的所有材质并列出它们的名称。”*
* *“创建一个模拟日落的方向光。”*

### 使用附件 (Attachments)
您可以通过附加文件为 AI 提供上下文：
1.  点击输入栏上方的 **+ File** 或 **+ Folder**。
2.  选择 C# 脚本、文本文档或说明文件。
3.  AI 将读取这些文件的内容，以便在生成代码前理解您的代码库。

### 控制台窗口
如果在设置中启用了 `Show Python Console`，将弹出一个独立的命令行窗口显示原始 Python 日志。否则，日志将重定向到内部的 Process Log 视图中。

---

## 技术细节

### 架构
系统采用本地客户端-服务器模型运行：
1.  **Unity (Client)**：`CopilotWindow` 通过 HTTP 发送 JSON 请求（提示词 + 附件）到 Python 服务器。
2.  **Python (Server)**：`ai_server.py` (Flask) 接收请求，使用 `SkillManager` 构建系统提示词，并查询 LLM。
3.  **代码生成**：LLM 返回封装在 Markdown 代码块中的 Python 代码。
4.  **执行桥接**：Python 服务器通过 TCP Socket (端口 8081) 连接回 Unity，并发送代码由 Unity 的内部 Python 引擎执行。

### 限制
* **执行安全**：AI 动态生成并运行代码。虽然 `unity.md` 提供了严格规则，但在执行破坏性的批量操作前，请务必备份项目。
* **上下文窗口**：附加过多的大型文件可能会超出所选 LLM 模型的 Token 限制。
* **Runtime vs Editor**：大多数技能专为 **Editor 编辑时** 自动化设计（依赖 `UnityEditor` 命名空间）。构建后的游戏可能无法使用这些功能。

---

## 故障排除

**"Cannot connect to Unity port 8081"**
* 确保 Unity 已打开且 `AiSkillsBridge` 已初始化。
* 尝试点击 "Restart Service" 以重新绑定 Socket。

**"ModuleNotFoundError: No module named..."**
* 系统会尝试自动安装依赖项。如果失败，请在终端手动运行 `pip install flask openai pyyaml`。

**AI 生成了代码但没有任何反应**
* 检查 **Process Log** 中是否有 "Unity Execution" 错误。
* AI 可能产生了“幻觉”，调用了 `Reference API` 中不存在的函数。请确保它遵循了 `unity.md` 中的核心规则（不直接调用参考函数）。