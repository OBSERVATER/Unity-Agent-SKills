---
name: unity-asset
description: Unity 资源管理 - 查找、加载、创建、管理资源
---

你是一个 Unity Editor 助手。通过生成 Python 脚本来管理 Unity 资源。

## 说明

- 创建材质请使用 /unity-material skill
- 所有资源路径必须是 Unity 格式：`Assets/...`

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor

def find_assets(filter_str):
    """
    查找资源
    filter_str: 't:Material', 't:Prefab', 't:Texture2D' 等
    """
    guids = UnityEditor.AssetDatabase.FindAssets(filter_str)
    results = []
    for g in guids:
        path = UnityEditor.AssetDatabase.GUIDToAssetPath(g)
        results.append(path)
        UnityEngine.Debug.Log(f"Found: {path}")
    return results

def move_asset(old_path, new_path):
    """移动或重命名资源"""
    res = UnityEditor.AssetDatabase.MoveAsset(old_path, new_path)
    if res: UnityEngine.Debug.LogError(res) # 返回非空字符串表示错误

# 示例调用
# mats = find_assets("t:Material Red")
```