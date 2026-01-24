---
name: unity-component
description: 组件操作参考 - 添加、获取、属性设置 (Rigidbody, Collider 等)
---

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor

def add_or_get_component(target_name, component_type_name):
    """
    添加组件逻辑参考
    component_type_name: 'Rigidbody', 'BoxCollider', 'AudioSource' 等
    """
    obj = UnityEngine.GameObject.Find(target_name)
    if not obj: return None
    
    # 利用 Python.NET 的反射机制或 getattr 获取类型
    # 注意：在生成的代码中，建议直接使用 UnityEngine.Rigidbody 这样的明确类型
    
    # 示例：添加刚体
    if component_type_name == "Rigidbody":
        comp = obj.GetComponent[UnityEngine.Rigidbody]()
        if not comp: comp = obj.AddComponent[UnityEngine.Rigidbody]()
        return comp
        
    # 示例：添加 BoxCollider
    elif component_type_name == "BoxCollider":
        comp = obj.GetComponent[UnityEngine.BoxCollider]()
        if not comp: comp = obj.AddComponent[UnityEngine.BoxCollider]()
        return comp
        
    return None

def configure_rigidbody(target_name, mass=1.0, use_gravity=True, is_kinematic=False):
    """配置刚体属性参考"""
    obj = UnityEngine.GameObject.Find(target_name)
    if not obj: return
    
    rb = obj.GetComponent[UnityEngine.Rigidbody]()
    if rb:
        rb.mass = mass
        rb.useGravity = use_gravity
        rb.isKinematic = is_kinematic

# 示例调用
# add_or_get_component("Player", "Rigidbody")
# configure_rigidbody("Player", mass=5.0)
```