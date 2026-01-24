# Unity Skills 测试用例 Prompt 集合

## 使用说明

这个文档包含了13个Unity技能文件的测试prompt。每个prompt都设计用来验证该技能的核心功能。

---

## 1. /unity - 基础导航和概览

### 测试 Prompt
```
请使用 /unity 技能展示如何：
1. 创建一个新的 GameObject 并命名为 "TestObject"
2. 添加一个 Rigidbody 组件到该对象
3. 设置 Rigidbody 的质量为 2.5
4. 打印出对象的名称和组件信息

返回完整的 Python 代码示例。
```

### 预期验证点
- [x] 代码能正确导入 UnityEngine 和 UnityEditor
- [x] GameObject 创建和查找逻辑正确
- [x] 组件添加和属性设置正确
- [x] 对象销毁逻辑正确

---

## 2. /unity-animator - 动画系统

### 测试 Prompt
```
使用 /unity-animator 技能创建一个完整的动画控制脚本：

场景：游戏角色有以下动画状态：
- "Idle" (空闲)
- "Run" (奔跑)
- "Jump" (跳跃)
- "Attack" (攻击)

需要实现：
1. 加载 Animator Controller
2. 根据输入参数切换状态
3. 设置 Speed 参数控制动画播放速度
4. 实现跳跃时触发 "Jump" 触发器
5. 在攻击时播放攻击动画并监听完成事件

提供完整的 Python 实现代码。
```
错误原因： AI 生成的 Python 代码尝试使用 from UnityEngine import AnimatorParameterType 这种写法。 在 Python.NET 环境中，C# 的 Enum（枚举） 有时无法像普通 Python 类那样直接从命名空间中 import 出来。

解决方案： 你需要让 AI 不要 使用 from UnityEngine import AnimatorParameterType，而是直接使用 全名 UnityEngine.AnimatorParameterType。

### 预期验证点
- [ ] Animator 控制器加载正确
- [ ] SetBool/SetFloat/SetInteger/SetTrigger 方法使用正确
- [ ] 状态转换逻辑正确
- [ ] 参数命名与实际 Controller 匹配

---

## 3. /unity-asset - 资源管理

### 测试 Prompt
```
使用 /unity-asset 技能创建以下资源管理功能：

1. 在 Assets/Materials 文件夹中创建 3 个材质：
   - "RedMaterial" (红色)
   - "GreenMaterial" (绿色)
   - "BlueMaterial" (蓝色)
   使用 URP pipeline (Universal Render Pipeline/Lit shader)

2. 创建一个函数，查找所有材质并列出它们的名称

3. 实现一个批量更新函数，将所有材质的 Metallic 值设为 0.5

4. 最后清理：删除这些材质文件

返回完整代码并验证文件夹结构。
```

### 预期验证点
- [x] 文件夹创建逻辑正确
- [x] Material 创建和保存正确
- [x] URP shader 支持工作正常
- [x] 属性设置逻辑正确
- [x] AssetDatabase 操作正确

---

## 4. /unity-component - 组件管理

### 测试 Prompt
```
使用 /unity-component 技能实现以下场景：

创建一个名为 "PhysicsObject" 的 GameObject，并：

1. 添加以下组件：
   - Rigidbody (质量 = 3.0, 重力 = true)
   - BoxCollider (大小 = (2, 2, 2))
   - AudioSource (音量 = 0.8)

2. 创建一个函数列出该对象的所有组件及其启用状态

3. 创建一个函数修改 Rigidbody 的拖拽值为 0.2

4. 最后创建一个验证函数，检查是否所有必需的物理组件都存在

返回完整的 Python 代码和执行结果。
```

### 预期验证点
- [x] 多个组件的添加正确
- [x] 组件属性设置正确
- [x] 组件查询和遍历逻辑正确
- [x] HasComponent 检查逻辑正确

---

## 5. /unity-debug - 调试工具

### 测试 Prompt
```
使用 /unity-debug 技能创建完整的调试系统：

1. 创建一个调试日志函数，支持：
   - 普通信息 (蓝色)
   - 警告信息 (黄色)
   - 错误信息 (红色)
   - 成功信息 (绿色)

2. 使用 EditorPrefs 保存和读取以下配置：
   - 调试模式开关 (DebugModeEnabled)
   - 日志级别 (LogLevel)
   - 最后运行时间 (LastRunTime)

3. 创建一个函数在场景中绘制调试线，显示一个立方体的边界

4. 记录应用程序内存使用情况

返回完整代码和测试输出。
```

### 预期验证点
- [ ] Debug.Log/Warning/Error 调用正确
- [ ] EditorPrefs 读写逻辑正确
- [ ] DrawLine/DrawRay 绘制正确
- [ ] 内存监控代码正确

---

## 6. /unity-editor - 编辑器控制

### 测试 Prompt
```
使用 /unity-editor 技能实现以下编辑器自动化：

1. 创建一个函数在编辑器中进入播放模式，等待 3 秒后退出

2. 实现选中某个特定对象的功能 (如 "Main Camera")

3. 创建一个菜单项 "Tools/Test/Print Scene Info"，
   点击时打印当前场景的所有对象信息

4. 实现一个编译检查函数，验证脚本是否有编译错误

5. 保存场景的当前状态

返回完整的编辑器脚本代码。
```

### 预期验证点
- [ ] EditorApplication.isPlaying 状态控制正确
- [ ] Selection.activeGameObject 选择逻辑正确
- [ ] EditorUtility.ExecuteMenuItem 菜单调用正确
- [ ] 编译状态检查正确

---

## 7. /unity-gameobject - 游戏对象管理

### 测试 Prompt
```
使用 /unity-gameobject 技能实现以下场景：

1. 创建一个 GameObject 树结构：
   - 根节点：Player
     - 子节点：Body
     - 子节点：Weapon
       - 子节点：Bullet (预制体实例)

2. 实现查找：
   - 按名称查找 "Weapon"
   - 按标签查找所有 "Enemy" 标签的对象
   - 按类型查找所有 Rigidbody

3. 实现变换：
   - 移动 Player 到位置 (5, 0, 10)
   - 旋转 Player 90 度
   - 缩放 Body 为 (1.5, 1.5, 1.5)

4. 挂载csharp脚本实现对象池模式，预生成 10 个子弹，使用时激活，回收时禁用

返回完整的对象管理系统代码。
```

### 预期验证点
- [ ] GameObject 创建和层级结构正确
- [ ] Find/FindWithTag/FindObjectsOfType 逻辑正确
- [ ] Transform 位置/旋转/缩放操作正确
- [ ] 对象池实现逻辑正确

---

## 8. /unity-light - 灯光系统

### 测试 Prompt
```
使用 /unity-light 技能创建完整的灯光场景：

1. 创建 4 种不同类型的灯光：
   - Directional (方向光)：位置 (0, 10, 0)，强度 1.2
   - Point (点光)：位置 (5, 2, 0)，范围 15，强度 1.5
   - Spot (聚光)：位置 (-5, 2, 0)，范围 20，角度 45，强度 2.0
   - Area (面光)：位置 (0, 5, 5)，大小 (2, 2)

2. 为每个灯光设置不同的颜色：
   - 红色，绿色，蓝色，黄色

3. 实现一个函数修改所有灯光的强度为参数值

4. 创建一个灯光组管理系统，支持启用/禁用整组灯光

5. 验证灯光阴影设置是否正确

返回完整的灯光管理系统代码。
```

### 预期验证点
- [ ] 4 种灯光类型创建正确
- [ ] 灯光属性设置正确
- [ ] Color 设置逻辑正确
- [ ] 灯光分组管理逻辑正确

---

## 9. /unity-material - 材质系统 (URP 支持)

### 测试 Prompt
```
使用 /unity-material 技能创建完整的材质管理系统：

1. 创建 5 个不同的材质，支持 URP 和 Built-in 两种管线：
   - 标准 PBR 材质 (Metallic=0.8, Roughness=0.2)
   - 无光照材质 (纯色)
   - 纹理材质 (带贴图)
   - 透明材质 (Alpha=0.5)
   - 自发光材质 (Emissive)

2. 验证材质是否正确适配当前渲染管线

3. 实现一个函数批量应用材质到多个对象

4. 创建材质变体系统，支持保存不同的配置

5. 测试材质属性的实时修改

返回完整的材质管理系统和测试报告。
```

### 预期验证点
- [ ] URP 和 Built-in shader 检测正确
- [ ] 属性名称映射正确 (_BaseColor vs _Color)
- [ ] 纹理加载和设置正确
- [ ] 材质变体创建逻辑正确

---

## 10. /unity-prefab - 预制体系统

### 测试 Prompt
```
使用 /unity-prefab 技能实现以下预制体工作流：

1. 创建一个 Prefab：
   - 名称：Enemy
   - 包含：Mesh, Rigidbody, BoxCollider, Animator, AudioSource

2. 实现以下操作：
   - 实例化 5 个 Enemy 预制体在不同位置
   - 修改实例的某个属性 (颜色)
   - 应用更改回预制体
   - 还原实例到预制体原始状态

3. 创建一个预制体版本管理系统：
   - 保存预制体的当前版本
   - 记录变更历史
   - 支持版本回滚

4. 批量操作：删除所有 Enemy 实例并清理预制体

返回完整的预制体管理系统代码。
```

### 预期验证点
- [ ] PrefabUtility 操作正确
- [ ] 预制体创建和保存正确
- [ ] 实例化逻辑正确
- [ ] 应用/还原操作正确
- [ ] 版本管理系统设计合理

---

## 11. /unity-project - 项目管理

### 测试 Prompt
```
使用 /unity-project 技能实现以下项目管理功能：

1. 扫描项目结构并生成统计：
   - 总文件数
   - 文件类型分布 (Scene, Prefab, Material, Script 等)
   - 项目占用空间

2. 创建标准项目文件夹结构：
   - Assets/Scenes
   - Assets/Prefabs
   - Assets/Materials
   - Assets/Scripts
   - Assets/Audio
   - Assets/Textures
   - Assets/UI

3. 实现文件管理功能：
   - 移动某个资源到新位置
   - 重命名资源
   - 复制资源为副本
   - 删除未使用的资源

4. 创建项目报告：
   - 列出所有大文件 (>5MB)
   - 找出重复资源
   - 检查断开的引用

返回完整的项目管理系统和报告。
```

### 预期验证点
- [ ] AssetDatabase 文件操作正确
- [ ] 文件夹创建和管理正确
- [ ] 资源统计逻辑正确
- [ ] 文件移动/重命名/复制逻辑正确
- [ ] 项目分析功能正确

---

## 12. /unity-scene - 场景管理

### 测试 Prompt
```
使用 /unity-scene 技能实现以下场景管理功能：

1. 创建并管理多个场景：
   - "MainMenu"
   - "Level1"
   - "Level2"
   - "Settings"

2. 实现场景导航：
   - 从 MainMenu 加载 Level1
   - 实现异步加载场景
   - 实现多场景共存 (MainMenu + Level1)

3. 创建场景分析工具：
   - 列出场景中的所有对象
   - 计算总对象数
   - 统计各类型对象数量
   - 生成场景层级树

4. 实现场景保存和版本管理：
   - 修改场景后保存
   - 另存为新版本
   - 对比场景差异

5. 创建 Build Settings 配置：
   - 添加所有场景到 Build
   - 配置场景顺序

返回完整的场景管理系统代码。
```

### 预期验证点
- [ ] EditorSceneManager 场景操作正确
- [ ] SceneManager 运行时加载正确
- [ ] 异步加载逻辑正确
- [ ] 场景层级遍历逻辑正确
- [ ] Build 配置正确

---

## 13. /unity-validation - 验证工具

### 测试 Prompt
```
使用 /unity-validation 技能创建完整的项目验证系统：

1. 场景验证：
   - 检查是否有缺失脚本的对象
   - 检查空 GameObject
   - 检查重复命名的对象
   - 检查未连接的预制体引用

2. 资源验证：
   - 扫描所有材质是否使用正确的 Shader
   - 检查纹理的导入设置是否优化
   - 找出未使用的资源
   - 检查资源依赖关系

3. 修复工具：
   - 自动清理空文件夹
   - 移除缺失脚本引用
   - 优化大纹理
   - 重新导入损坏的资源

4. 生成验证报告：
   - 问题总数
   - 按类型分类的问题列表
   - 优化建议
   - 修复前后对比

5. 性能检查：
   - 检查 Draw Call 优化
   - 检查是否有重复的材质

返回完整的验证和修复系统代码，以及测试报告。
```

### 预期验证点
- [ ] 场景扫描逻辑正确
- [ ] 资源检测逻辑正确
- [ ] 问题报告生成正确
- [ ] 自动修复功能正确
- [ ] 性能分析逻辑正确

---

## 运行测试的步骤

### 方式 1: 逐个测试
```
1. 选择其中一个测试 Prompt
2. 复制 Prompt 文本
3. 在 Claude Code 中调用相应的技能
4. 执行生成的代码
5. 验证结果是否符合预期
```

### 方式 2: 批量自动化测试
```python
# 创建一个主测试脚本，自动执行所有测试
from UnityEditor import AssetDatabase
import datetime

class SkillTester:
    def __init__(self):
        self.results = []
    
    def run_all_tests(self):
        """运行所有技能测试"""
        tests = [
            ("unity", test_unity),
            ("unity-animator", test_animator),
            # ... 其他测试
        ]
        
        for skill_name, test_func in tests:
            try:
                result = test_func()
                self.results.append({
                    "skill": skill_name,
                    "status": "PASS" if result else "FAIL",
                    "timestamp": datetime.datetime.now()
                })
            except Exception as e:
                self.results.append({
                    "skill": skill_name,
                    "status": "ERROR",
                    "error": str(e),
                    "timestamp": datetime.datetime.now()
                })
        
        self.print_report()
    
    def print_report(self):
        """打印测试报告"""
        print("=" * 50)
        print("SKILL TESTING REPORT")
        print("=" * 50)
        for result in self.results:
            print(f"{result['skill']}: {result['status']}")
        print("=" * 50)

# 运行测试
tester = SkillTester()
tester.run_all_tests()
```

---

## 测试结果记录模板

```markdown
# 测试结果报告

## 技能名称: /unity-xxx

| 项目 | 结果 | 备注 |
|------|------|------|
| 基础功能 | ✅/❌ | |
| 错误处理 | ✅/❌ | |
| 性能 | ✅/❌ | |
| 文档完整性 | ✅/❌ | |

### 发现的问题
1. ...
2. ...

### 修复建议
1. ...
2. ...

### 总体评分
- 功能完整性: 9/10
- 代码质量: 8/10
- 文档清晰度: 9/10
- 总体: 8.7/10
```

---

## 快速参考

| 技能 | 测试难度 | 预估时间 | 关键验证点 |
|------|---------|---------|-----------|
| /unity | 简单 | 5分钟 | 基础导入和创建 |
| /unity-animator | 中等 | 15分钟 | 状态机和参数控制 |
| /unity-asset | 中等 | 15分钟 | 文件创建和管理 |
| /unity-component | 简单 | 10分钟 | 组件操作 |
| /unity-debug | 简单 | 10分钟 | 日志和可视化 |
| /unity-editor | 中等 | 15分钟 | 编辑器自动化 |
| /unity-gameobject | 中等 | 15分钟 | 对象创建和查询 |
| /unity-light | 简单 | 10分钟 | 灯光创建和属性 |
| /unity-material | 中等 | 20分钟 | URP 兼容性 |
| /unity-prefab | 难 | 25分钟 | 预制体操作 |
| /unity-project | 难 | 25分钟 | 项目分析 |
| /unity-scene | 难 | 25分钟 | 场景管理 |
| /unity-validation | 难 | 30分钟 | 验证和修复 |

**总预估时间**: 约 3.5-4 小时完整测试
