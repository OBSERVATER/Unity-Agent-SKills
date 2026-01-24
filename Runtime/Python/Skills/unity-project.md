---
name: unity-project
description: Unity 项目管理 - 项目结构、资源文件操作
---

## API 速查表

- **文件操作**
  - `AssetDatabase.MoveAsset(oldPath, newPath)`
    - **说明**: 移动或重命名资源。
    - **返回**: `string` (错误信息，成功则为空字符串)。
  - `AssetDatabase.CopyAsset(path, newPath)`
    - **说明**: 复制资源文件。
    - **返回**: `bool` (是否成功)。
  - `AssetDatabase.DeleteAsset(path)`
    - **说明**: 删除资源文件。
    - **返回**: `bool` (是否成功)。
  - `AssetDatabase.CreateFolder(parentFolder, newFolderName)`
    - **说明**: 创建新文件夹（会自动生成 .meta）。
    - **返回**: `string` (新文件夹的 GUID)。
  - `AssetDatabase.Refresh()`
    - **说明**: 强制 Unity 刷新资源数据库，检测文件变动。

- **系统与元数据**
  - `AssetImporter.GetAtPath(path)`
    - **说明**: 获取资源的导入器设置（如纹理设置、模型设置）。
    - **返回**: `AssetImporter` 对象。
  - `os.walk(path)` / `os.listdir(path)`
    - **说明**: Python 标准库，用于遍历文件系统结构。

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor
import os

def ensure_project_structure(folders):
    """
    确保项目文件夹结构存在
    folders: list of paths relative to Assets (e.g. ['Scripts/Core', 'Prefabs'])
    """
    assets_path = UnityEngine.Application.dataPath
    
    for folder in folders:
        # 使用 Unity API 创建文件夹以生成 meta 文件
        # 路径处理需要拆分 'Assets/A/B' -> CreateFolder('Assets/A', 'B')
        
        full_path = os.path.join(assets_path, folder)
        if os.path.exists(full_path): continue
        
        # 递归创建父级较为复杂，这里假设父级存在或使用简单逻辑
        # 简单实现：使用 Python os 创建目录，然后 Refresh
        try:
            os.makedirs(full_path, exist_ok=True)
        except:
            UnityEngine.Debug.LogError(f"Failed to create {full_path}")
            
    UnityEditor.AssetDatabase.Refresh()

def cleanup_empty_folders(root_folder="Assets"):
    """
    递归清理空文件夹
    """
    import shutil
    
    deleted = False
    assets_path = UnityEngine.Application.dataPath
    target_path = os.path.join(assets_path, root_folder.replace("Assets/", "")) if root_folder != "Assets" else assets_path

    for root, dirs, files in os.walk(target_path, topdown=False):
        for name in dirs:
            dir_path = os.path.join(root, name)
            # 检查是否为空（忽略 meta 文件）
            has_files = any(f for f in os.listdir(dir_path) if not f.endswith(".meta") and f != ".DS_Store")
            if not has_files:
                try:
                    # 使用 AssetDatabase 删除以处理 meta
                    relative_path = "Assets" + dir_path.replace(assets_path, "").replace("\\", "/")
                    UnityEditor.AssetDatabase.DeleteAsset(relative_path)
                    deleted = True
                except:
                    pass
                    
    if deleted:
        UnityEditor.AssetDatabase.Refresh()

# 示例调用
# ensure_project_structure(["Scripts", "Materials", "Prefabs"])
# cleanup_empty_folders()
```