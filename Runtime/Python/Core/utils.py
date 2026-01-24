import sys
import subprocess
import importlib.util
import re
import os

def check_and_install(package_name, import_name=None):
    """
    检查 Python 包是否已安装，未安装则尝试通过清华源自动安装。
    :param package_name: pip 安装时的包名 (如 PyYAML)
    :param import_name: import 时的模块名 (如 yaml)，若为 None 则默认同 package_name
    """
    if import_name is None: 
        import_name = package_name
    
    if importlib.util.find_spec(import_name) is None:
        print(f"[System] Installing missing package: {package_name}...")
        try: 
            subprocess.check_call([
                sys.executable, "-m", "pip", "install", 
                package_name, "-i", "https://pypi.tuna.tsinghua.edu.cn/simple"
            ])
        except subprocess.CalledProcessError: 
            print(f"[System] Failed to install {package_name}")
        except Exception as e:
            print(f"[System] Error installing {package_name}: {e}")

def parse_frontmatter(content):
    """
    解析 Markdown 文件头部的 YAML Frontmatter。
    格式:
    ---
    key: value
    ---
    正文内容
    """
    # 延迟导入 yaml，防止在 install 之前调用
    import yaml 
    
    match = re.match(r'^---\s*\n(.*?)\n---\s*\n(.*)$', content, re.DOTALL)
    if match:
        try: 
            return yaml.safe_load(match.group(1)), match.group(2)
        except: 
            return {}, content
    return {}, content

def extract_python_code(raw_text):
    """
    从 LLM 返回的文本中提取 Python 代码块。
    优先匹配 ```python ... ```，其次匹配 ``` ... ```
    """
    # 尝试匹配 ```python
    pattern = r"```python\s*(.*?)\s*```"
    match = re.search(pattern, raw_text, re.DOTALL)
    if match: 
        return match.group(1).strip()
    
    # 尝试匹配通用代码块 ```
    pattern_generic = r"```\s*(.*?)\s*```"
    match_generic = re.search(pattern_generic, raw_text, re.DOTALL)
    if match_generic: 
        return match_generic.group(1).strip()
    
    return None

def process_attachments(paths):
    """
    读取附件文件内容并格式化为 Prompt 上下文。
    """
    if not paths: return ""
    ctx = "\n### Attachments:\n"
    for p in paths:
        if os.path.exists(p) and os.path.isfile(p):
            try: 
                with open(p, 'r', encoding='utf-8') as f: 
                    ctx += f"\n--- {p} ---\n{f.read()}\n"
            except: 
                pass
    return ctx