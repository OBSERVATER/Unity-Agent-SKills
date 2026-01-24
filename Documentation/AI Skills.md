# AI Skills (`com.observater.aiskills`)

**AI Skills** is a Unity Editor extension that integrates Large Language Models (LLMs) directly into your workflow. It allows you to control the Unity Editor using natural language commands by bridging Unity's C# environment with a local Python execution engine.

## Features

* **Natural Language Control**: Create GameObjects, materials, lights, and UI elements simply by describing them.
* **Python.NET Integration**: Executes AI-generated Python code directly within Unity, allowing access to the full `UnityEngine` and `UnityEditor` API.
* **Skill-Based Architecture**: Modular `.md` skill files define capabilities (Scene, Asset, Component management), making the AI context-aware and easily extensible.
* **Context Awareness**: Attach project files or folders to your prompt to give the AI specific context about your scripts or assets.
* **Customizable Backend**: Configurable API endpoint (defaults to DeepSeek), model selection, and execution ports.

---

## Installation

### Prerequisites
1.  **Unity Version**: 2021.3 or later (Recommended).
2.  **Python**: Python 3.8+ installed and added to your system PATH.
3.  **Internet Connection**: Required to communicate with the LLM API (e.g., DeepSeek, OpenAI).

## Getting Started

### 1. Open the Copilot Window
Go to the Unity Menu bar and select:
**Tools > AI Copilot**

### 2. Configure the Service
In the Copilot Window, expand the **Settings** foldout:
* **API Key**: Enter your LLM provider's API Key (e.g., `sk-xxxx`).
* **Base URL**: Default is `https://api.deepseek.com`. Change this if using OpenAI or a local LLM.
* **Model**: Default is `deepseek-coder`.
* **Python Port**: Default `5000` (Server) / `8081` (Unity Execution).

### 3. Start the Server
Click the **Restart Service** button in the toolbar.
* This launches the background Python server (`ai_server.py`).
* Watch the **Process Log** at the bottom of the window for `[System] Unity Socket Server Started`.

---

## Usage Guide

### Basic Commands
Type your request in the chat input field and press **Send** (or Enter).

* *"Create a red cube named 'Player' and add a Rigidbody to it."*
* *"Find all materials in the project and list their names."*
* *"Create a directional light simulating a sunset."*

### Using Attachments
You can provide context to the AI by attaching files:
1.  Click **+ File** or **+ Folder** above the input bar.
2.  Select C# scripts, text files, or documentation.
3.  The AI will read these files to understand your specific codebase before generating a response.

### The Console Window
If enabled in settings (`Show Python Console`), a separate command window will open to display raw Python logs. Otherwise, logs are redirected to the internal Process Log view.

---

## Technical Details

### Architecture
The system operates using a localized client-server model:
1.  **Unity (Client)**: The `CopilotWindow` sends JSON requests (Prompt + Attachments) to the Python Server via HTTP.
2.  **Python (Server)**: `ai_server.py` (Flask) receives the request, builds a System Prompt using `SkillManager`, and queries the LLM.
3.  **Code Generation**: The LLM returns Python code wrapped in markdown blocks.
4.  **Execution Bridge**: The Python Server connects back to Unity via a TCP Socket (Port 8081) and sends the code to be executed by Unity's internal Python engine.

### Limitations
* **Execution Safety**: The AI generates and runs code dynamically. While the `unity.md` skill provides strict rules, always backup your project before running destructive bulk operations.
* **Context Window**: Attaching too many large files may exceed the token limit of the selected LLM model.
* **Runtime vs Editor**: Most skills are designed for **Editor time** automation (`UnityEditor` namespace). They may not function if the game is built.

---

## Troubleshooting

**"Cannot connect to Unity port 8081"**
* Ensure Unity is open and the `AiSkillsBridge` has initialized.
* Try clicking "Restart Service" to re-bind the socket.

**"ModuleNotFoundError: No module named..."**
* The system tries to auto-install dependencies. If this fails, manually run `pip install flask openai pyyaml` in your terminal.

**AI generates code but nothing happens**
* Check the **Process Log** for "Unity Execution" errors.
* The AI might have hallucinated a function not present in the `Reference API`. Ensure `unity.md` rules are being followed.