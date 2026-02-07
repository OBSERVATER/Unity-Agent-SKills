import sys
import subprocess
import importlib.util
import re
import os

# 定义常见的二进制扩展名黑名单，避免读取
BINARY_EXTENSIONS = {
    '.dll', '.exe', '.so', '.dylib', '.png', '.jpg', '.jpeg', '.tga', '.psd', 
    '.fbx', '.obj', '.blend', '.unity', '.asset', '.prefab', '.mat', '.meta', 
    '.cache', '.pdf', '.zip', '.7z'
}

def check_and_install(package_name, import_name=None):
    """
    检查 Python 包是否已安装，未安装则尝试通过清华源自动安装。
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
    """
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
    """
    pattern = r"```python\s*(.*?)\s*```"
    match = re.search(pattern, raw_text, re.DOTALL)
    if match: 
        return match.group(1).strip()
    
    pattern_generic = r"```\s*(.*?)\s*```"
    match_generic = re.search(pattern_generic, raw_text, re.DOTALL)
    if match_generic: 
        return match_generic.group(1).strip()
    
    return None

def is_binary_file(filepath):
    """
    简单判断是否为二进制文件：
    1. 检查扩展名
    2. 尝试读取前 1024 字节，若包含 null byte 则视为二进制
    """
    # 1. 扩展名检查
    _, ext = os.path.splitext(filepath)
    if ext.lower() in BINARY_EXTENSIONS:
        return True

    # 2. 内容检查 (读取前 1KB)
    try:
        with open(filepath, 'rb') as f:
            chunk = f.read(1024)
            if b'\0' in chunk:
                return True
    except:
        pass # 读取失败视为不可读
    
    return False

def process_attachments(paths, project_root=None):
    """
    读取附件文件内容并格式化为 Prompt 上下文。
    :param paths: 文件路径列表 (可以是相对路径)
    :param project_root: Unity 项目根目录绝对路径，用于解析相对路径
    """
    if not paths: return ""
    
    ctx = "\n\n### User Provided Files:\n"
    valid_count = 0

    for p in paths:
        # 路径解析：如果是相对路径，且提供了 project_root，则拼接
        full_path = p
        if project_root and not os.path.isabs(p):
            full_path = os.path.join(project_root, p)
        
        if os.path.exists(full_path) and os.path.isfile(full_path):
            # 过滤二进制文件
            if is_binary_file(full_path):
                print(f"[Warn] Skipped binary file: {os.path.basename(p)}")
                continue

            try: 
                with open(full_path, 'r', encoding='utf-8') as f: 
                    content = f.read()
                    # 获取文件扩展名用于 Markdown 语法高亮
                    _, ext = os.path.splitext(p)
                    ext_tag = ext.lstrip('.').lower() or "text"
                    
                    ctx += f"\nFile: {p}\n```{ext_tag}\n{content}\n```\n"
                    valid_count += 1
            except UnicodeDecodeError:
                print(f"[Warn] Skipped non-utf-8 file: {p}")
            except Exception as e: 
                print(f"[Error] Failed to read {p}: {e}")
    
    return ctx if valid_count > 0 else ""