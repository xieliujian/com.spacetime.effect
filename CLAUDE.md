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
│       │   ├── BoxProjectedCubemapDefine.cs  Shader 属性名常量
│       │   └── ReflectionProbeParam.cs       探针包围盒同步组件
│       └── SunShaft/                — 太阳光轴运行时
│           ├── SunShaftsDefine.cs       Shader 名称/属性 ID/角度常量
│           ├── SunShafts.cs             VolumeComponent 参数定义
│           ├── SunShaftsFeatureV2.cs    ScriptableRendererFeature 入口
│           ├── SunShaftsPass.cs         ScriptableRenderPass 渲染实现
│           ├── SunShaftsProperties.cs   Feature Inspector 参数
│           └── SunShaftUtil.cs          Shader/Material 工具函数
├── Editor/
│   └── Scripts/
│       └── BoxProjectedCubemap/
│           └── BoxProjectedCubemapTool.cs  编辑器右键菜单工具（类名 BoxProjectedCubemapDirection）
├── Shaders/
│   ├── BoxProjectedCubemap/         — 盒子投影 Shader
│   │   ├── BoxCubeRefl.shader           主 Shader 入口
│   │   ├── BoxCubeReflInclude.hlsl      CBUFFER / 采样器声明
│   │   ├── BoxCubeReflUtils.hlsl        盒体射线求交函数
│   │   └── BoxCubeReflForwardPass.hlsl  顶点/片元着色器
│   ├── SunShaft/                    — 太阳光轴三步 Pass Shader
│   │   ├── BuildSkyForBlurShader.shader
│   │   ├── DirectionalBlurShader.shader
│   │   ├── FinalBlendShader.shader
│   │   └── SimpleNoise.hlsl             噪声函数库
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
- `ReflectionProbeParam`（`[ExecuteInEditMode]` MonoBehaviour）挂在 ReflectionProbe 所在 GameObject 上，每帧自动将包围盒的 `center` / `min` / `max` 同步到三个公共字段（`reflProbeCenter` / `reflProbeBoxMin` / `reflProbeBoxMax`）。
- `BoxProjectedCubemapDefine`（静态类）集中定义 Shader 属性名字符串常量，供运行时与编辑器工具共用，避免散落的魔法字符串：

```csharp
BoxProjectedCubemapDefine.s_Shader_BoxCubeRefl_UseBoxCubeRefl_PropId  // "_UseBoxCubeRefl"
BoxProjectedCubemapDefine.s_Shader_BoxCubeRefl_Center_PropId          // "_BoxCubeReflCenter"
BoxProjectedCubemapDefine.s_Shader_BoxCubeRefl_BoxMin_PropId          // "_BoxCubeReflBoxMin"
BoxProjectedCubemapDefine.s_Shader_BoxCubeRefl_BoxMax_PropId          // "_BoxCubeReflBoxMax"
```

**编辑器工具**（文件 `BoxProjectedCubemapTool.cs`，类名 `BoxProjectedCubemapDirection`）
- `GameObject > BoxProjectedCubemapDirection_Copy` — 记录选中 GameObject 上的全部组件（`GetComponents<Component>()`）。
- `GameObject > BoxProjectedCubemapDirection_Paste` — 从记录的组件中找出 `ReflectionProbeParam`，将其包围盒数据通过 `BoxProjectedCubemapDefine` 常量写入选中物件的 `sharedMaterial`；对每个目标 GameObject 独立处理，任一材质属性写入成功才标记 `dirty`。

**关键约束**
- Paste 前必须先 Copy，否则 `s_CopiedComponents` 为 null，Paste 静默跳过。
- Copy 来源中若无 `ReflectionProbeParam` 组件，Paste 同样静默跳过。
- 写入材质后调用 `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets` 确保持久化。
- `_UseBoxCubeRefl` 在 Paste 时被设置为 `1`（float），该属性不在 `BoxCubeReflInclude.hlsl` 的 `CBUFFER` 中，由 `BoxCubeRefl.shader` Properties 块单独声明。

**HLSL Shader Include 链**

```
BoxCubeRefl.shader
  └── BoxCubeReflForwardPass.hlsl
        └── BoxCubeReflUtils.hlsl
              └── BoxCubeReflInclude.hlsl   ← CBUFFER / sampler 声明根文件
```

- `BoxCubeReflInclude.hlsl`：`UnityPerMaterial` CBUFFER（`_MainTex_ST`、`_BoxCubeReflCenter/Min/Max`、`_BlendPercent`）与 `_MainTex` / `_ReflectionCube` 采样器。
- `BoxCubeReflUtils.hlsl`：`BoxProjectedCubemapDirectionCustom()` — 将世界反射方向与盒体 AABB 射线求交，输出视差修正后的 cubemap 采样方向。
- `BoxCubeReflForwardPass.hlsl`：URP Forward 顶点 / 片元着色器；片元阶段固定使用水平法线 `(0,1,0)` 计算反射，最终以 `_BlendPercent` 混合主贴图与反射颜色（`_BlendPercent→1` 趋向主贴图，`→0` 趋向反射）。

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

SunShaftsDefine                          — Shader 名称/属性 ID/角度常量（集中定义）
SunShafts（VolumeComponent）             — Volume Override 参数容器
SunShaftsProperties                      — Feature Inspector 序列化参数
SunShaftUtil                             — Material 懒加载工具（GetMaterial / GetShaderMaterial）
```

**可见性裁剪规则**（角度常量定义在 `SunShaftsDefine`）
- 摄像机前向与主光源方向夹角 > `SunShaftsDefine.s_CanVisibleRenderLightAngle`（30°）时跳过渲染。
- 摄像机前向与世界 Up 向量夹角 > `SunShaftsDefine.s_CanVisibleRenderUpAngle`（70°）时跳过渲染。
- `forceOn = true` 时绕过所有角度裁剪。

**`SunShaftsDefine` 常量速查**

| 常量 | 类型 | 值 / 说明 |
|------|------|-----------|
| `s_BuildSkyShaderName` | `string` | `"SpaceTime/PostProcess/SunShaft/BuildSkyForBlurShader"` |
| `s_BlurShaderName` | `string` | `"SpaceTime/PostProcess/SunShaft/DirectionalBlurShader"` |
| `s_FinalBlendShaderName` | `string` | `"SpaceTime/PostProcess/SunShaft/FinalBlendShader"` |
| `s_CommandBufferName` | `static readonly string` | `"ShaftsRendering"` |
| `s_CanVisibleRenderLightAngle` | `static readonly float` | `30.0f` |
| `s_CanVisibleRenderUpAngle` | `static readonly float` | `70.0f` |
| `s_Shader_SunPosition_PropId` | `int` | `Shader.PropertyToID("_SunPosition")` |
| `s_Shader_BlurStep_PropId` | `int` | `Shader.PropertyToID("_BlurStep")` |
| `s_Shader_Intensity_PropId` | `int` | `Shader.PropertyToID("_Intensity")` |
| `s_Shader_ShaftsColor_PropId` | `int` | `Shader.PropertyToID("_ShaftsColor")` |
| `s_Shader_SunThresholdSky_PropId` | `int` | `Shader.PropertyToID("_SunThresholdSky")` |
| `s_Shader_SkyNoiseScale_PropId` | `int` | `Shader.PropertyToID("_SkyNoiseScale")` |
| `s_Shader_UseStencilMaskTex_PropId` | `int` | `Shader.PropertyToID("_UseStencilMaskTex")` |
| `s_Shader_StencilMaskTex_PropId` | `int` | `Shader.PropertyToID("_StencilMaskTex")` |

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
| 公共/内部静态字段（`static readonly`） | `s_` 前缀 + PascalCase | `s_CanVisibleRenderLightAngle`, `s_BuildSkyShaderName` |
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
