---
name: unity-editor
description: Unity 编辑器控制 - 播放模式、选择、编译、菜单和资源刷新
---

## API 速查表 (API Reference)

- **编辑器状态 (Editor State)**
  - `EditorApplication.isPlaying`
    - **说明**: 读写属性，控制进入或退出播放模式。
  - `EditorApplication.isPaused`
    - **说明**: 读写属性，控制暂停状态。
  - `EditorApplication.ExecuteMenuItem(path)`
    - **说明**: 执行菜单项 (如 "File/Save Project")。
  - `EditorUtility.RequestScriptReload()`
    - **说明**: 强制触发脚本重新编译。

- **选择与上下文 (Selection & Context)**
  - `Selection.objects`
    - **说明**: 获取或设置当前选中的所有对象 (Object[])。
  - `Selection.activeGameObject`
    - **说明**: 获取或设置当前活动的游戏物体。
  - `AssetDatabase.Refresh()`
    - **说明**: 刷新资源数据库。

- **实用工具 (Utilities)**
  - `EditorUtility.RevealInFinder(path)`
    - **说明**: 在文件资源管理器中显示文件。
  - `Application.unityVersion`
    - **说明**: 获取当前 Unity 版本号。

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor
# 必须导入 System 以支持 C# 数组创建
import System 

def control_play_mode(action="toggle"):
    """
    控制播放模式
    action: 'play', 'stop', 'toggle'
    """
    if action == "play":
        UnityEditor.EditorApplication.isPlaying = True
    elif action == "stop":
        UnityEditor.EditorApplication.isPlaying = False
    elif action == "toggle":
        UnityEditor.EditorApplication.isPlaying = not UnityEditor.EditorApplication.isPlaying

def execute_menu_item(menu_path):
    """
    执行编辑器菜单项
    例如: 'Assets/Refresh', 'File/Save Project', 'GameObject/Create Empty'
    """
    UnityEditor.EditorApplication.ExecuteMenuItem(menu_path)

def set_selection(target_names):
    """
    设置编辑器选中项
    target_names: 字符串列表，物体名称
    """
    objects = []
    for name in target_names:
        obj = UnityEngine.GameObject.Find(name)
        if obj: objects.append(obj)
    
    # 将 Python list 转换为 C# Object[] 数组
    if objects:
        # 创建特定类型的 C# 数组
        arr = System.Array.CreateInstance(UnityEngine.Object, len(objects))
        for i, o in enumerate(objects):
            arr.SetValue(o, i)
        UnityEditor.Selection.objects = arr
    else:
        # 清空选择
        UnityEditor.Selection.objects = None

def request_compilation():
    """强制重新编译脚本"""
    UnityEditor.EditorUtility.RequestScriptReload()
    UnityEngine.Debug.Log("Script compilation requested.")

# 示例调用
# control_play_mode("stop")
# execute_menu_item("File/Save Project")