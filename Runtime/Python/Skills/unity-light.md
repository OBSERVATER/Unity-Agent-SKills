---
name: unity-light
description: Unity 灯光创建和设置 - 方向光、点光源、聚光灯和区域光
---

## API 速查表 (API Reference)

- **灯光类型 (LightType)**
  - `Directional`: 平行光，用于模拟太阳
  - `Point`: 点光源，用于模拟灯泡或火焰
  - `Spot`: 聚光灯，用于模拟手电筒或舞台灯
  - `Area`: 区域光，仅用于烘焙照明

- **核心参数 (Properties)**
  - `color`: 灯光颜色
  - `intensity`: 光照强度
  - `range`: 光照范围
  - `spotAngle`: 聚光角度
  - `shadows`: 阴影类型 (None, Hard, Soft)
  - `cookie`: 遮光图纹理

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor

def create_light(name, kind="Point", color="#FFFFFF", intensity=1.0, range_val=10.0):
    """
    创建灯光
    kind: Directional, Point, Spot, Area
    """
    obj = UnityEngine.GameObject(name)
    light_comp = obj.AddComponent[UnityEngine.Light]()
    
    # 设置类型
    if kind == "Directional":
        light_comp.type = UnityEngine.LightType.Directional
        obj.transform.rotation = UnityEngine.Quaternion.Euler(50, -30, 0)
    elif kind == "Spot":
        light_comp.type = UnityEngine.LightType.Spot
        light_comp.range = range_val
        light_comp.spotAngle = 30.0
    elif kind == "Area":
        light_comp.type = UnityEngine.LightType.Area
        light_comp.shape = UnityEngine.LightShape.Rectangle
    else:
        # 默认为 Point
        light_comp.type = UnityEngine.LightType.Point
        light_comp.range = range_val
        
    # 设置通用属性
    col = UnityEngine.ColorUtility.TryParseHtmlString(color, UnityEngine.Color.white)[1]
    light_comp.color = col
    light_comp.intensity = intensity
    
    # 设置阴影 (默认开启软阴影)
    if kind != "Area":
        light_comp.shadows = UnityEngine.LightShadows.Soft
    
    return obj

def set_light_shadows(target_name, shadow_type="Soft"):
    """
    设置阴影类型
    shadow_type: None, Hard, Soft
    """
    obj = UnityEngine.GameObject.Find(target_name)
    if not obj: return
    
    light = obj.GetComponent[UnityEngine.Light]()
    if not light: return
    
    if shadow_type == "Hard":
        light.shadows = UnityEngine.LightShadows.Hard
    elif shadow_type == "None":
        light.shadows = UnityEngine.LightShadows.None
    else:
        light.shadows = UnityEngine.LightShadows.Soft

# 示例调用
# create_light("MainSun", "Directional", intensity=1.5)
# create_light("Torch", "Point", color="#FFCC00", range_val=15)