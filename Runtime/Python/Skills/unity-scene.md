---
name: unity-scene
description: Unity 场景管理 - 新建、保存、加载场景
---

## 场景管理操作参考表

- **场景操作 (Editor)**
  - `EditorSceneManager.NewScene(setup)`
    - **说明**: 创建一个新场景。
    - **返回**: `Scene` 结构体。
  - `EditorSceneManager.SaveScene(scene, path)`
    - **说明**: 保存指定场景，如果不传路径则覆盖。
    - **返回**: `bool` (是否成功)。
  - `EditorSceneManager.OpenScene(path, mode)`
    - **说明**: 加载指定路径的场景文件。
    - **返回**: `Scene` 结构体。

- **运行时/通用操作 (Runtime/Common)**
  - `SceneManager.GetActiveScene()`
    - **说明**: 获取当前活跃的场景。
    - **返回**: `Scene` 结构体。
  - `SceneManager.SetActiveScene(scene)`
    - **说明**: 将指定场景设置为活跃场景（用于多场景编辑）。
    - **返回**: `bool` (是否成功)。
  - `SceneManager.sceneCount`
    - **说明**: 获取当前已加载的场景数量。
    - **返回**: `int`。

- **场景数据 (Scene Data)**
  - `scene.GetRootGameObjects()`
    - **说明**: 获取场景根节点下的所有物体。
    - **返回**: `Array[GameObject]`。

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor
import UnityEditor.SceneManagement 
import UnityEngine.SceneManagement

def save_current_scene(path=None):
    """保存当前场景"""
    scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene()
    if path:
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path)
    else:
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)

def new_scene():
    """新建场景"""
    UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects)

def open_scene(path):
    """打开场景"""
    if System.IO.File.Exists(path):
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path)

# 示例调用
# save_current_scene("Assets/Scenes/Level1.unity")
```