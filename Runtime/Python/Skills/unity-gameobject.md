---
name: unity-gameobject
description: GameObject 操作参考 - 创建、查找、变换、层级
---

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor

def create_object(name, primitive_type=None, parent_name=None):
    """
    创建物体逻辑参考
    primitive_type: 'Cube', 'Sphere', 'Capsule', 'Cylinder', 'Plane', 'Quad' 或 None (空物体)
    """
    if primitive_type:
        # 解析基础类型枚举
        p_type = getattr(UnityEngine.PrimitiveType, primitive_type, UnityEngine.PrimitiveType.Cube)
        obj = UnityEngine.GameObject.CreatePrimitive(p_type)
    else:
        obj = UnityEngine.GameObject()
    
    obj.name = name
    
    if parent_name:
        parent = UnityEngine.GameObject.Find(parent_name)
        if parent:
            obj.transform.SetParent(parent.transform)
            
    return obj

def set_transform(target_name, pos=None, rot=None, scale=None):
    """
    设置变换逻辑参考
    pos/rot/scale: (x, y, z) tuple
    """
    obj = UnityEngine.GameObject.Find(target_name)
    if not obj: return
    
    if pos: obj.transform.position = UnityEngine.Vector3(*pos)
    if rot: obj.transform.rotation = UnityEngine.Quaternion.Euler(*rot)
    if scale: obj.transform.localScale = UnityEngine.Vector3(*scale)

def delete_object(name):
    """删除物体逻辑参考"""
    obj = UnityEngine.GameObject.Find(name)
    if obj:
        UnityEngine.Object.DestroyImmediate(obj)

# 示例调用 (AI 应根据用户输入生成类似的调用)
# obj = create_object("MyPlayer", "Capsule")
# set_transform("MyPlayer", pos=(0, 1, 0))
```