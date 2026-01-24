---
name: unity-animator
description: Unity 动画控制器管理
---

## API 速查表 (API Reference)

- **运行时组件 (Animator Component)**
  - `animator.Play(stateName, layer, normalizedTime)`
    - **说明**: 立即播放指定状态。
  - `animator.CrossFade(stateName, duration)`
    - **说明**: 在指定时间内平滑过渡到新状态。
  - `animator.SetBool(name, val)` / `SetFloat` / `SetInteger` / `SetTrigger`
    - **说明**: 设置运行时参数值。
  - `animator.GetBool(name)` / `GetFloat` / `GetInteger`
    - **说明**: 获取当前参数值。

- **编辑器资源 (AnimatorController Asset)**
  - `AnimatorController.CreateAnimatorControllerAtPath(path)`
    - **说明**: 在 Assets 目录下创建新的 `.controller` 文件。
  - `controller.AddParameter(name, type)`
    - **说明**: 向控制器添加参数 (Float, Int, Bool, Trigger)。
  - `controller.AddLayer(name)`
    - **说明**: 添加新的动画层。

- **关键枚举 (Enums)**
  - `AnimatorControllerParameterType`: `Float`, `Int`, `Bool`, `Trigger`

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor
# 显式导入动画相关的编辑器命名空间
import UnityEditor.Animations

def setup_animator_controller(target_name, controller_path):
    """
    为物体设置 Animator Controller
    """
    obj = UnityEngine.GameObject.Find(target_name)
    if not obj: return
    
    anim = obj.GetComponent[UnityEngine.Animator]()
    if not anim: anim = obj.AddComponent[UnityEngine.Animator]()
    
    # 加载或创建 Controller
    # 注意: AnimatorController 类位于 UnityEditor.Animations 命名空间下
    ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath(controller_path, UnityEditor.Animations.AnimatorController)
    
    if not ctrl:
        # 确保路径目录存在
        folder = System.IO.Path.GetDirectoryName(controller_path)
        if not System.IO.Directory.Exists(folder):
            System.IO.Directory.CreateDirectory(folder)
            
        ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controller_path)
        UnityEngine.Debug.Log(f"Created new Animator Controller: {controller_path}")
        
    anim.runtimeAnimatorController = ctrl
    return ctrl

def add_parameter(controller, param_name, param_type_str):
    """
    添加动画参数
    param_type_str: 'Float', 'Int', 'Bool', 'Trigger'
    """
    if not controller: return

    # 映射类型字符串到 UnityEngine 枚举
    type_map = {
        "Float": UnityEngine.AnimatorControllerParameterType.Float,
        "Int": UnityEngine.AnimatorControllerParameterType.Int,
        "Bool": UnityEngine.AnimatorControllerParameterType.Bool,
        "Trigger": UnityEngine.AnimatorControllerParameterType.Trigger
    }
    
    p_type = type_map.get(param_type_str)
    
    # 检查参数是否已存在
    for p in controller.parameters:
        if p.name == param_name:
            return # 已存在则跳过
            
    if p_type is not None:
        controller.AddParameter(param_name, p_type)
        UnityEngine.Debug.Log(f"Added parameter '{param_name}' ({param_type_str})")

# 示例调用
# ctrl = setup_animator_controller("Player", "Assets/Animations/Player.controller")
# add_parameter(ctrl, "Speed", "Float")
# add_parameter(ctrl, "IsGround", "Bool")