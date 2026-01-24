---
name: unity-validation
description: Unity 验证工具 - 场景验证、资源检查、完整性验证
---

## API 速查表

- `check_missing_scripts()`
  - **参数**: 无
  - **返回**: `int` (发现的丢失脚本数量)
  - **说明**: 遍历当前场景所有物体，查找并记录 "Missing Script" 的组件。

- `validate_active_scene()`
  - **参数**: 无
  - **返回**: `void`
  - **说明**: 对当前活动场景执行一整套基础验证逻辑（如检查空物体、丢失脚本）。

- `find_empty_objects()`
  - **参数**: 无
  - **返回**: `List[GameObject]`
  - **说明**: 查找场景中除了 Transform 外没有任何组件的空物体（通常是废弃节点）。

### 维护操作 (Maintenance)
- `cleanup_empty_folders(root_path: str)`
  - **参数**: `root_path` (默认 "Assets") - 扫描的根目录
  - **返回**: `bool` (执行是否成功)
  - **说明**: 递归扫描并删除项目中的空文件夹（需要小心使用）。

- `find_unused_assets(folder: str)`
  - **参数**: `folder` (默认 "Assets") - 扫描目录
  - **返回**: `List[str]` (未引用资源的路径列表)
  - **说明**: 基于 AssetDatabase 的依赖分析，查找未被引用的资源。

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor

def check_missing_scripts():
    """检查场景中丢失脚本的物体"""
    all_objs = UnityEngine.Resources.FindObjectsOfTypeAll[UnityEngine.GameObject]()
    count = 0
    for obj in all_objs:
        # 只检查场景物体，排除 Asset
        if obj.hideFlags == UnityEngine.HideFlags.NotEditable or obj.hideFlags == UnityEngine.HideFlags.HideAndDontSave:
            continue
        if UnityEditor.EditorUtility.IsPersistent(obj.transform.root.gameObject):
            continue

        components = obj.GetComponents[UnityEngine.Component]()
        for i in range(len(components)):
            if components[i] == None: # Python.NET 中 null 检查
                UnityEngine.Debug.LogWarning(f"Missing Script on: {obj.name}", obj)
                count += 1
    
    UnityEngine.Debug.Log(f"Validation Finished. Found {count} missing scripts.")

# 示例调用
# check_missing_scripts()
```