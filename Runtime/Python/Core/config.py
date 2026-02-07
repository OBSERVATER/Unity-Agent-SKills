import os

# --- 基础配置 ---
# Unity 监听的执行端口
UNITY_EXEC_PORT = 8081
# Unity 主机地址
UNITY_HOST = '127.0.0.1'

# --- AI 模型默认配置 ---
DEFAULT_API_KEY = "sk-placeholder"
DEFAULT_API_BASE = "https://api.deepseek.com"
DEFAULT_MODEL = "deepseek-coder"

# --- 路径配置 ---
# 获取当前文件 (config.py) 所在目录 -> .../Runtime/Python/Core
CURRENT_DIR = os.path.dirname(os.path.abspath(__file__))

# 技能文件夹路径 -> .../Runtime/Python/Skills
# 解析：从 Scripts 目录往上退一级，进入 Skills 目录
SKILLS_DIR = os.path.abspath(os.path.join(CURRENT_DIR, "..", "Skills"))

SHOW_RAW_RESPONSE = True