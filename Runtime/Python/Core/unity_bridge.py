import socket
import time
import json
import textwrap
from config import UNITY_HOST, UNITY_EXEC_PORT

def execute_in_unity(code):
    print(f"[Debug] Connecting to Unity ({UNITY_HOST}:{UNITY_EXEC_PORT})...")
    
    sock = None
    for attempt in range(3):
        try:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.settimeout(300)
            sock.connect((UNITY_HOST, UNITY_EXEC_PORT))
            break
        except ConnectionRefusedError:
            time.sleep(0.2)
            sock = None
        except Exception as e:
            return {"status": "error", "message": f"Connect Error: {e}"}

    if sock is None: 
        return {"status": "error", "message": f"Cannot connect to Unity port {UNITY_EXEC_PORT}."}

    try:
        # --- 核心代码包装器 ---
        # 这里的 Python 代码是在 Unity 内部运行的
        wrapped_code = f"""
import clr
import System
from System import AppDomain
import json
import traceback
import sys
import types 

# 1. 尝试加载 Assembly
try: clr.AddReference("Observater.AiSkills")
except: pass

# 2. 反射查找类型
bridge_type = None
for asm in AppDomain.CurrentDomain.GetAssemblies():
    t = asm.GetType("Observater.AiSkills.Runtime.Core.AiSkillsBridge")
    if t:
        bridge_type = t
        try: clr.AddReference(asm.FullName.split(',')[0]); break
        except: pass

# 3. 导入真实 Bridge
_RealBridge = None
try:
    from Observater.AiSkills.Runtime.Core import AiSkillsBridge as _RealBridge
except ImportError:
    try:
        from Observater.AiSkills import AiSkillsBridge as _RealBridge
    except:
        pass # 稍后处理

import UnityEngine
import UnityEditor

# 4. 定义代理类
_sent = False
class _BridgeProxy:
    @staticmethod
    def SendSuccess(m):
        global _sent
        if not _sent:
            if _RealBridge: _RealBridge.SendMessage(str(m))
            else: print(f"[Fallback] Success: {{m}}")
            _sent = True
    @staticmethod
    def SendMessage(m): _BridgeProxy.SendSuccess(m)
    @staticmethod
    def SendResult(m): _BridgeProxy.SendSuccess(m)
    @staticmethod
    def SendError(m):
        global _sent
        if not _sent:
            if _RealBridge: _RealBridge.SendError(str(m))
            else: print(f"[Fallback] Error: {{m}}")
            _sent = True
    @staticmethod
    def get_Config(): return _RealBridge.Config if _RealBridge else None

# ==========================================
# [关键修复] 模块欺骗 (Module Mocking)
# ==========================================

# 1. 修复 'No module named AiSkillsBridge'
if "AiSkillsBridge" not in sys.modules:
    _mock_mod = types.ModuleType("AiSkillsBridge")
    _mock_mod.SendSuccess = _BridgeProxy.SendSuccess
    _mock_mod.SendMessage = _BridgeProxy.SendMessage
    _mock_mod.SendResult = _BridgeProxy.SendResult
    _mock_mod.SendError = _BridgeProxy.SendError
    sys.modules["AiSkillsBridge"] = _mock_mod

# 2. 修复 'No module named unity_editor' (AI 幻觉兼容)
if "unity_editor" not in sys.modules:
    sys.modules["unity_editor"] = UnityEditor
if "unity_engine" not in sys.modules:
    sys.modules["unity_engine"] = UnityEngine

# 3. 全局变量注入
AiSkillsBridge = _BridgeProxy

print("[Internal] Running user code...")
try:
{textwrap.indent(code, '    ')}
except Exception as e:
    err = traceback.format_exc()
    print(f"[Internal] Execution Error: {{err}}")
    AiSkillsBridge.SendError(f"Error: {{e}}\\n{{err}}")
finally:
    if not _sent: AiSkillsBridge.SendSuccess("Done.")
"""
        sock.sendall(wrapped_code.encode('utf-8'))
        resp = sock.recv(65536).decode('utf-8')
        sock.close()
        return json.loads(resp) if resp else {"status": "error", "message": "Empty response"}
    except Exception as e:
        return {"status": "error", "message": f"Comm Error: {e}"}