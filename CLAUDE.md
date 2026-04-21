# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity visual effects package (`com.spacetime.effect`) based on the Universal Render Pipeline (URP). It provides two post-processing / rendering effects: Box Projected Cubemap reflections and Sun Shaft volumetric lighting.

## Project Structure

### Effect Package (`Packages/com.spacetime.effect/`)

```
com.spacetime.effect/
├── Runtime/
│   └── Scripts/
│       ├── BoxProjectedCubemap/     — 盒子投影运行时
│       │   └── ReflectionProbeParam.cs
│       └── SunShaft/                — 太阳光轴运行时
│           ├── SunShafts.cs             VolumeComponent 参数定义
│           ├── SunShaftsFeatureV2.cs    ScriptableRendererFeature 入口
│           ├── SunShaftsPass.cs         ScriptableRenderPass 渲染实现
│           ├── SunShaftsProperties.cs   Feature Inspector 参数
│           └── SunShaftUtil.cs          Shader/Material 工具函数
├── Editor/
│   └── Scripts/
│       └── BoxProjectedCubemap/
│           └── BoxProjectedCubemapTool.cs  编辑器右键菜单工具
├── Shaders/
│   ├── BoxProjectedCubemap/         — 盒子投影 Shader
│   ├── SunShaft/                    — 太阳光轴三步 Pass Shader
│   └── Shaders.cs                   命名空间占位符
└── readme/
    ├── BoxProjectedCubemap/BoxProjectedCubemap.md
    └── SunShaft/SunShaft.md
```

### Assembly Structure

| Assembly | 说明 |
|----------|------|
| `com.spacetime.effect.runtime` | 运行时脚本（`ST.Effect`、`ST.Effect.URP` 命名空间） |
| `com.spacetime.effect.editor` | 编辑器工具（`ST.Effect` 命名空间，仅 Editor 平台） |
| `com.spacetime.effect.shaders` | Shader 相关脚本 |

## Architecture Patterns

### BoxProjectedCubemap

用于矩形场景的精确 Cubemap 反射投影。

**运行时**
- `ReflectionProbeParam`（`[ExecuteInEditMode]` MonoBehaviour）挂在 ReflectionProbe 所在 GameObject 上，每帧自动将包围盒的 `center` / `min` / `max` 同步到公共字段。

**编辑器工具**（`BoxProjectedCubemapTool`）
- `GameObject > BoxProjectedCubemapDirection_Copy` — 记录选中 GameObject 上的 `ReflectionProbeParam` 组件。
- `GameObject > BoxProjectedCubemapDirection_Paste` — 将记录的包围盒数据写入选中物件的 `sharedMaterial`，目标属性：`_UseBoxCubeRefl` / `_BoxCubeReflCenter` / `_BoxCubeReflBoxMin` / `_BoxCubeReflBoxMax`。

**关键约束**
- Paste 前必须先 Copy，否则 `s_CopiedComponents` 为 null，Paste 静默跳过。
- 写入材质后调用 `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets` 确保持久化。

---

### SunShaft 光轴

基于 URP ScriptableRendererFeature 的太阳光轴（体积光）后处理效果。

**调用链路**

```
SunShaftsFeatureV2（ScriptableRendererFeature）
  └── Create()         → 构造 SunShaftsPass
  └── AddRenderPasses() → 可见性裁剪后将 Pass 入队

SunShaftsPass（ScriptableRenderPass）
  └── Execute()
      ├── Pass 1: BuildSkyForBlurShader    — 天空区域采样 + 噪声
      ├── Pass 2: DirectionalBlurShader    — 径向模糊（多次迭代）
      └── Pass 3: FinalBlendShader         — 强度/颜色/遮罩混合回场景

SunShafts（VolumeComponent）             — Volume Override 参数容器
SunShaftsProperties                      — Feature Inspector 序列化参数
SunShaftUtil                             — Material 懒加载工具（GetMaterial / GetShaderMaterial）
```

**可见性裁剪规则**（`SunShaftsProperties`）
- 摄像机前向与主光源方向夹角 > `CAN_VISIBLE_RENDER_LIGHT_ANGLE`（30°）时跳过渲染。
- 摄像机前向与世界 Up 向量夹角 > `CAN_VISIBLE_RENDER_UP_ANGLE`（70°）时跳过渲染。
- `forceOn = true` 时绕过所有角度裁剪。

**Shader 名称常量**（定义在 `SunShaftsFeatureV2`）

```csharp
BUILD_SKY_SHADER_NAME   = "SpaceTime/PostProcess/SunShaft/BuildSkyForBlurShader"
BLUR_SHADER_NAME        = "SpaceTime/PostProcess/SunShaft/DirectionalBlurShader"
FINAL_BLEND_SHADER_NAME = "SpaceTime/PostProcess/SunShaft/FinalBlendShader"
```

**Volume 参数快速参考**（`SunShafts` VolumeComponent）

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `on` | `BoolParameter` | false | 启用效果 |
| `forceOn` | `BoolParameter` | false | 强制开启，跳过角度裁剪 |
| `intensity` | `ClampedFloatParameter` | 1.5 | 光轴强度（0–5） |
| `useSunLightColor` | `BoolParameter` | true | 使用主方向光颜色 |
| `shaftsColor` | `ColorParameter` | black | 自定义光轴颜色（HDR） |
| `useSunPosition` | `BoolParameter` | false | 手动指定太阳位置 |
| `sunPosition` | `Vector3Parameter` | zero | 自定义太阳世界坐标 |
| `sunThresholdSky` | `ClampedFloatParameter` | 0.75 | 天空采样范围阈值 |
| `depthDownscalePow2` | `ClampedIntParameter` | 3 | 降采样级别（0–4） |
| `blurRadius` | `FloatParameter` | 1.2 | 径向模糊半径 |
| `blurStepsCount` | `ClampedIntParameter` | 2 | 模糊迭代次数（1–4） |
| `useStencilMaskTex` | `BoolParameter` | false | 启用模板遮罩纹理 |
| `useRenderPassEvent` | `BoolParameter` | false | 自定义 RenderPass 插入时机 |

## Development Commands

### 构建
在 Unity Editor 中打开项目，确保已安装 URP。程序集会自动编译。

### 代码位置
- 运行时效果脚本：`Runtime/Scripts/`
- 编辑器工具：`Editor/Scripts/`
- Shader 文件：`Shaders/`
- 文档：`readme/`

## Coding Conventions

### Naming

| 类别 | 规则 | 示例 |
|------|------|------|
| 私有实例字段 | `m_` 前缀 + PascalCase | `m_ShaftsPass`, `m_ReflProbe` |
| 私有/内部静态字段 | `s_` 前缀 + PascalCase | `s_CopiedComponents` |
| 公共方法 | PascalCase | `Create`, `AddRenderPasses`, `GetMaterial` |
| 命名空间 | 效果通用 `ST.Effect`；URP 相关 `ST.Effect.URP` | — |

### Access Modifiers

**省略 `private` 关键字**，它是默认访问级别，无需显式声明：

```csharp
// 正确
static Component[] s_CopiedComponents;
SunShaftsPass m_ShaftsPass;
void DoSomething() { }

// 错误 — 不要写 explicit private
private static Component[] s_CopiedComponents;
private SunShaftsPass m_ShaftsPass;
private void DoSomething() { }
```

### Null Guard Style

Null 检查立即 return 时使用**两行形式**，return 后必须空一行：

```csharp
// 正确
if (props == null)
    return;

props.CacheSunShafts();

// 错误 — 缺少空行
if (props == null)
    return;
props.CacheSunShafts();

// 错误 — 不使用单行形式
if (props == null) return;
```

### Null-Conditional Operator (`?.`) — 禁止使用

```csharp
// 正确
if (m_ShaftsPass == null)
    return;

m_ShaftsPass.Setup(...);

// 错误
m_ShaftsPass?.Setup(...);
```

### XML Documentation Comments

**所有类、方法、字段、属性——无论访问修饰符——都必须添加 XML 文档注释，使用中文描述。**

#### 类

```csharp
/// <summary>
/// URP SunShaft 光轴效果的 ScriptableRendererFeature 入口，负责创建并注册渲染 Pass。
/// </summary>
public class SunShaftsFeatureV2 : ScriptableRendererFeature { ... }
```

#### 方法

```csharp
/// <summary>
/// 初始化 SunShaftsPass，在 Feature 首次创建时由 URP 调用。
/// </summary>
public override void Create() { ... }

/// <summary>
/// 懒加载指定名称的 Shader 并创建对应 Material；若 Shader 缺失则静默跳过。
/// </summary>
/// <param name="mat">待初始化的 Material 引用。</param>
/// <param name="shaderName">Shader 路径名称。</param>
public static void GetMaterial(ref Material mat, string shaderName) { ... }
```

#### 字段

```csharp
/// <summary>SunShaft 渲染 Pass 实例，由 Create() 构造。</summary>
SunShaftsPass m_ShaftsPass;

/// <summary>上一次 Copy 操作记录的组件列表，用于 Paste 时读取 ReflectionProbeParam。</summary>
static Component[] s_CopiedComponents;
```

#### 禁止项

- 禁止省略注释（包括私有字段）。
- 禁止使用英文注释（统一中文）。
- 禁止写无意义复述（如 `/// <summary>m_ShaftsPass 字段</summary>`）；注释必须说明**用途或约束**。

### Section Dividers（代码段分割）

较长文件中用统一注释分隔逻辑段：

```csharp
// ──────────────────────────────────────────
// 初始化 / Shader 资源
// ──────────────────────────────────────────
```

## Key Dependencies

- Unity 2020.3+
- Universal Render Pipeline (URP)
- `UnityEngine.Rendering.Universal`
