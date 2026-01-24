---
name: unity-material
description: Unity Shader 和 Material 操作 - 创建材质、查找Shader、设置属性
---

## API 速查表

- **常用 Shader**
  - **Built-in**
    - `Standard`: 标准 PBR 着色器
    - `Unlit/Color`: 无光照纯色
    - `Unlit/Texture`: 无光照贴图
    - `Sprites/Default`: 2D 精灵默认
    - `UI/Default`: UI 默认
  - **URP**
    - `Universal Render Pipeline/Lit`: 标准 PBR
    - `Universal Render Pipeline/Unlit`: 无光照
    - `Universal Render Pipeline/SimpleLit`: 简化光照 (用于移动端)
    - `Universal Render Pipeline/Particle/Lit`: 粒子光照

- **核心属性映射**
  - **基础颜色**: `_Color` (Built-in) / `_BaseColor` (URP)
  - **主贴图**: `_MainTex` (Built-in) / `_BaseMap` (URP)
  - **金属度**: `_Metallic` (通用)
  - **平滑度**: `_Glossiness` (Built-in) / `_Smoothness` (URP)
  - **法线贴图**: `_BumpMap` (通用)
  - **自发光**: `_EmissionColor` (通用)

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor
import os

def create_material(name, color_hex="#FFFFFF", shader_name=None, folder="Assets/Materials"):
    """
    创建材质逻辑参考。
    自动处理 URP (Universal Render Pipeline) 和 Built-in 的属性差异。
    """
    # 1. 确保目录存在
    if not UnityEditor.AssetDatabase.IsValidFolder(folder):
        UnityEditor.AssetDatabase.CreateFolder("Assets", "Materials") # 简化处理，假设根目录在 Assets
    
    # 2. 智能查找 Shader
    if not shader_name:
        # 尝试优先查找 URP Shader
        shader = UnityEngine.Shader.Find("Universal Render Pipeline/Lit")
        if not shader:
            shader = UnityEngine.Shader.Find("Standard")
    else:
        shader = UnityEngine.Shader.Find(shader_name)
        
    if not shader:
        UnityEngine.Debug.LogError(f"Shader not found: {shader_name}")
        return

    # 3. 创建材质资源
    mat = UnityEngine.Material(shader)
    
    # 4. 解析颜色 (Hex to Color)
    col = UnityEngine.ColorUtility.TryParseHtmlString(color_hex, UnityEngine.Color.white)[1]
    
    # 5. 智能设置属性 (URP vs Standard)
    if mat.HasProperty("_BaseColor"):
        mat.SetColor("_BaseColor", col) # URP
    elif mat.HasProperty("_Color"):
        mat.SetColor("_Color", col)     # Standard
        
    # 6. 保存
    path = f"{folder}/{name}.mat"
    UnityEditor.AssetDatabase.CreateAsset(mat, path)
    
    return path

def assign_material(target_name, material_path):
    """将材质应用到物体"""
    obj = UnityEngine.GameObject.Find(target_name)
    mat = UnityEditor.AssetDatabase.LoadAssetAtPath(material_path, UnityEngine.Material)
    
    if obj and mat:
        renderer = obj.GetComponent[UnityEngine.Renderer]()
        if renderer:
            renderer.material = mat

# 示例调用
# path = create_material("RedMat", "#FF0000")
# assign_material("Cube", path)
```