# URP SceneGray 代码移植设计

**日期：** 2026-07-14  
**状态：** 已确认  
**源工程：** `D:\xieliujian\UnityDemo_UrpSceneGray\Assets`  
**目标包：** `Packages/com.spacetime.effect`

## 目标

将 Demo 工程中的场景灰度切换 C# 代码移植到 `com.spacetime.effect`，加入命名空间，并按 LingRen C# 规范与包内现有模式整理。

## 范围

### 包含

- 移植 Runtime：`UrpSceneGray.cs`
- 移植 Editor：`UrpSceneGrayEditor.cs`
- 新增常量定义：`SceneGrayDefine.cs`
- 命名空间：`ST.Effect`
- 按 LingRen 规范整理（UTF-8 BOM、命名、大括号、去掉无用代码等）

### 不包含

- Volume Profile / URP Asset / 场景资源
- 改造成 VolumeComponent / ScriptableRendererFeature
- 新建 asmdef（沿用现有 `com.spacetime.effect.runtime` / `com.spacetime.effect.editor`）

## 文件布局

```
Runtime/Scripts/SceneGray/
  SceneGrayDefine.cs
  UrpSceneGray.cs

Editor/Scripts/SceneGray/
  UrpSceneGrayEditor.cs
```

目录风格对齐现有 `SunShaft/`、`BoxProjectedCubemap/`。

## 命名空间与程序集

| 文件 | 命名空间 | 程序集 |
|------|----------|--------|
| `SceneGrayDefine.cs` | `ST.Effect` | runtime |
| `UrpSceneGray.cs` | `ST.Effect` | runtime |
| `UrpSceneGrayEditor.cs` | `ST.Effect` | editor |

Editor 与 Runtime 同命名空间，与 `BoxProjectedCubemapTool` 一致。

## 组件职责

### `SceneGrayDefine`（static）

集中存放灰度开关参数，风格对齐 `SunShaftsDefine` / `BoxProjectedCubemapDefine`：

- `public static readonly float s_GrayPostExposure = -0.1f;`
- `public static readonly float s_GrayContrast = 40f;`
- `public static readonly float s_GrayHueShift = 0f;`
- `public static readonly float s_GraySaturation = -100f;`
- `public static readonly float s_NormalPostExposure = 0f;`
- `public static readonly float s_NormalContrast = 0f;`
- `public static readonly float s_NormalHueShift = 0f;`
- `public static readonly float s_NormalSaturation = 0f;`

### `UrpSceneGray`（MonoBehaviour）

- 挂在带 `Volume` 的 GameObject 上
- `SwitchEff()`：切换内部灰度状态，读写 `volume.sharedProfile` 上的 `ColorAdjustments`
- 灰度开：写入 `s_Gray*`；灰度关：写入 `s_Normal*`
- 去掉源码中空的 `Start` / `Update` 与无用 using
- 成员顺序：私有字段 → 公有方法 → 系统回调（`OnGUI`）
- 继续使用 `sharedProfile`（与源 Demo 行为一致）
- 空引用早退范围与源一致：`Volume == null`、`TryGet<ColorAdjustments>` 失败时直接 return；不额外发明守卫，不引入新的日志依赖
- 保留 `OnGUI` 文案「运行时查看效果」（Demo 对等；不删除）

### `UrpSceneGrayEditor`（CustomEditor）

- Inspector 按钮文案：「切换效果」
- 点击后调用 `UrpSceneGray.SwitchEff()` 并 `Repaint()`

## 行为契约

| 状态 | postExposure | contrast | hueShift | saturation |
|------|--------------|----------|----------|------------|
| 灰度开 | -0.1 | 40 | 0 | -100 |
| 灰度关 | 0 | 0 | 0 | 0 |

运行时切换逻辑与源 Demo 一致；仅组织方式与规范变化。

## 代码规范要点

- UTF-8 with BOM
- 4 空格缩进；`if` 的 `{` 换行
- 不写不必要的 `private`
- 静态只读字段使用 `s_` 前缀（Define 文件）
- 私有实例字段使用 `m_` 前缀（如 `m_Gray`）
- 行宽 ≤ 120

## 验收标准

1. 三个 `.cs` 文件出现在上述路径，命名空间均为 `ST.Effect`
2. `UrpSceneGray.SwitchEff()` 只从 `SceneGrayDefine` 取值，无魔法数
3. Editor 按钮显示「切换效果」，可切换灰度
4. 不修改 asmdef、不引入资源资产、不改变包内其他效果代码
