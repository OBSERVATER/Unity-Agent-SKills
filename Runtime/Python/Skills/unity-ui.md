---
name: unity-ui
description: Unity UI 元素创建和设置
---

## API 速查表

- **容器 (Containers)**
  - `Canvas`
    - **组件**: `UnityEngine.Canvas`, `CanvasScaler`, `GraphicRaycaster`
    - **属性**: `renderMode`, `scaleFactor`
    - **说明**: UI 渲染的根节点，所有 UI 元素必须在此层级下。
  - `Panel`
    - **组件**: `UnityEngine.UI.Image`
    - **说明**: 基础容器，通常铺满背景 (Stretch/Stretch)。
  - `ScrollRect`
    - **组件**: `UnityEngine.UI.ScrollRect`
    - **属性**: `content` (RectTransform), `horizontal` (bool), `vertical` (bool)
    - **说明**: 滚动视图区域，通常包含 Mask 和 Scrollbar。

- **可视元素 (Visuals)**
  - `Text`
    - **组件**: `UnityEngine.UI.Text`
    - **属性**: `text` (str), `fontSize` (int), `color` (Color), `font` (Font)
    - **说明**: 显示文本内容。
  - `Image`
    - **组件**: `UnityEngine.UI.Image`
    - **属性**: `sprite` (Sprite), `color` (Color), `raycastTarget` (bool)
    - **说明**: 显示图片/Sprite，也作为交互组件的点击目标。

- **交互控件 (Interactions)**
  - `Button`
    - **组件**: `UnityEngine.UI.Button`
    - **属性**: `onClick` (Event), `interactable` (bool)
    - **说明**: 按钮组件，通常挂载在 Image 上。
  - `InputField`
    - **组件**: `UnityEngine.UI.InputField`
    - **属性**: `text`, `placeholder`, `characterLimit`
    - **说明**: 文本输入框。
  - `Slider` / `Toggle` / `Dropdown`
    - **组件**: `UI.Slider`, `UI.Toggle`, `UI.Dropdown`
    - **说明**: 分别用于数值滑动、开关切换和下拉选择。

## 常用操作参考实现

```python
import UnityEngine
import UnityEditor
import UnityEngine.UI

def create_ui_text(text_content="Hello"):
    """创建基础 UI 结构 (Canvas -> Text)"""
    # 查找或创建 Canvas
    canvas = UnityEngine.GameObject.FindObjectOfType[UnityEngine.Canvas]()
    if not canvas:
        canvas_obj = UnityEngine.GameObject("Canvas")
        canvas = canvas_obj.AddComponent[UnityEngine.Canvas]()
        canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay
        canvas_obj.AddComponent[UnityEngine.UI.CanvasScaler]()
        canvas_obj.AddComponent[UnityEngine.UI.GraphicRaycaster]()
    
    # 创建 Text
    txt_obj = UnityEngine.GameObject("DynamicText")
    txt_obj.transform.SetParent(canvas.transform, False)
    
    txt = txt_obj.AddComponent[UnityEngine.UI.Text]()
    txt.text = text_content
    txt.font = UnityEngine.Resources.GetBuiltinResource[UnityEngine.Font]("Arial.ttf")
    txt.color = UnityEngine.Color.black
    txt.alignment = UnityEngine.TextAnchor.MiddleCenter
    
    # 设置 RectTransform
    rect = txt_obj.GetComponent[UnityEngine.RectTransform]()
    rect.sizeDelta = UnityEngine.Vector2(200, 50)
    rect.anchoredPosition = UnityEngine.Vector2.zero

# 示例调用
# create_ui_text("Welcome to Unity")
```