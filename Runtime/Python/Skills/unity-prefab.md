---
name: unity-prefab
description: 预制体操作参考 - 保存 Prefab、实例化 Prefab
---

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor

def save_as_prefab(target_name, folder="Assets/Prefabs"):
    """将场景物体保存为 Prefab"""
    obj = UnityEngine.GameObject.Find(target_name)
    if not obj: return
    
    if not UnityEditor.AssetDatabase.IsValidFolder(folder):
        UnityEditor.AssetDatabase.CreateFolder("Assets", "Prefabs")
        
    local_path = f"{folder}/{target_name}.prefab"
    
    # 确保路径唯一
    local_path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(local_path)
    
    # 创建 Prefab
    UnityEditor.PrefabUtility.SaveAsPrefabAssetAndConnect(obj, local_path, UnityEditor.ActionMode.kModeToNewPrefab)
    UnityEngine.Debug.Log(f"Prefab saved: {local_path}")

def instantiate_prefab(prefab_path, pos=(0,0,0)):
    """实例化 Prefab"""
    prefab = UnityEditor.AssetDatabase.LoadAssetAtPath(prefab_path, UnityEngine.GameObject)
    if prefab:
        instance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab)
        instance.transform.position = UnityEngine.Vector3(*pos)
        return instance

# 示例调用
# save_as_prefab("Player")
# instantiate_prefab("Assets/Prefabs/Enemy.prefab", (5, 0, 5))
```