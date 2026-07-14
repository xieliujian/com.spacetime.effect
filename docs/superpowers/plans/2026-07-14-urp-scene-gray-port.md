# URP SceneGray Port Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port Demo `UrpSceneGray` C# into `com.spacetime.effect` under `ST.Effect`, with `SceneGrayDefine` holding gray/normal ColorAdjustments values.

**Architecture:** Three files under `SceneGray/` folders (Runtime Define + MonoBehaviour, Editor CustomEditor). Reuse existing asmdefs. No assets, no VolumeComponent refactor.

**Tech Stack:** Unity C#, URP Volume `ColorAdjustments`, LingRen C# style (`s_` / `m_`, brace style).

**Spec:** `docs/superpowers/specs/2026-07-14-urp-scene-gray-port-design.md`

---

## File Structure

| File | Responsibility |
|------|----------------|
| `Runtime/Scripts/SceneGray/SceneGrayDefine.cs` | `static readonly` gray/normal parameters |
| `Runtime/Scripts/SceneGray/UrpSceneGray.cs` | Toggle Volume ColorAdjustments |
| `Editor/Scripts/SceneGray/UrpSceneGrayEditor.cs` | Inspector switch button |

---

### Task 1: SceneGrayDefine

**Files:**
- Create: `Runtime/Scripts/SceneGray/SceneGrayDefine.cs`

- [x] **Step 1: Create Define file (UTF-8 BOM)**

```csharp
namespace ST.Effect
{
    /// <summary>
    /// SceneGray 效果的常量定义，集中管理灰度开/关时的 ColorAdjustments 参数。
    /// </summary>
    public static class SceneGrayDefine
    {
        /// <summary>灰度开启：曝光。</summary>
        public static readonly float s_GrayPostExposure = -0.1f;

        /// <summary>灰度开启：对比度。</summary>
        public static readonly float s_GrayContrast = 40f;

        /// <summary>灰度开启：色相偏移。</summary>
        public static readonly float s_GrayHueShift = 0f;

        /// <summary>灰度开启：饱和度。</summary>
        public static readonly float s_GraySaturation = -100f;

        /// <summary>灰度关闭：曝光。</summary>
        public static readonly float s_NormalPostExposure = 0f;

        /// <summary>灰度关闭：对比度。</summary>
        public static readonly float s_NormalContrast = 0f;

        /// <summary>灰度关闭：色相偏移。</summary>
        public static readonly float s_NormalHueShift = 0f;

        /// <summary>灰度关闭：饱和度。</summary>
        public static readonly float s_NormalSaturation = 0f;
    }
}
```

- [x] **Step 2: Commit**

```bash
git add Runtime/Scripts/SceneGray/SceneGrayDefine.cs
git commit -m "feat(SceneGray): add SceneGrayDefine parameters"
```

---

### Task 2: UrpSceneGray runtime

**Files:**
- Create: `Runtime/Scripts/SceneGray/UrpSceneGray.cs`
- Source: `UnityDemo_UrpSceneGray/Assets/Scripts/UrpSceneGray.cs`

- [x] **Step 1: Create runtime MonoBehaviour (UTF-8 BOM)**

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ST.Effect
{
    /// <summary>
    /// 通过 Volume ColorAdjustments 切换场景灰度效果。
    /// </summary>
    public class UrpSceneGray : MonoBehaviour
    {
        bool m_Gray;

        /// <summary>
        /// 切换灰度效果开/关。
        /// </summary>
        public void SwitchEff()
        {
            Volume volume = GetComponent<Volume>();
            if (volume == null)
            {
                return;
            }

            ColorAdjustments colorAdjust = null;
            volume.sharedProfile.TryGet<ColorAdjustments>(out colorAdjust);
            if (colorAdjust == null)
            {
                return;
            }

            m_Gray = !m_Gray;

            if (m_Gray)
            {
                colorAdjust.postExposure.value = SceneGrayDefine.s_GrayPostExposure;
                colorAdjust.contrast.value = SceneGrayDefine.s_GrayContrast;
                colorAdjust.hueShift.value = SceneGrayDefine.s_GrayHueShift;
                colorAdjust.saturation.value = SceneGrayDefine.s_GraySaturation;
            }
            else
            {
                colorAdjust.postExposure.value = SceneGrayDefine.s_NormalPostExposure;
                colorAdjust.contrast.value = SceneGrayDefine.s_NormalContrast;
                colorAdjust.hueShift.value = SceneGrayDefine.s_NormalHueShift;
                colorAdjust.saturation.value = SceneGrayDefine.s_NormalSaturation;
            }
        }

        void OnGUI()
        {
            GUIStyle fontStyle = new GUIStyle();
            fontStyle.normal.textColor = Color.white;
            fontStyle.fontSize = 50;

            GUI.Label(new Rect(0, 0, 600, 300), "运行时查看效果", fontStyle);
        }
    }
}
```

- [x] **Step 2: Commit**

```bash
git add Runtime/Scripts/SceneGray/UrpSceneGray.cs
git commit -m "feat(SceneGray): port UrpSceneGray runtime component"
```

---

### Task 3: UrpSceneGrayEditor

**Files:**
- Create: `Editor/Scripts/SceneGray/UrpSceneGrayEditor.cs`
- Source: `UnityDemo_UrpSceneGray/Assets/Scripts/Editor/UrpSceneGrayEditor.cs`

- [x] **Step 1: Create editor (UTF-8 BOM)**

```csharp
using UnityEngine;
using UnityEditor;

namespace ST.Effect
{
    /// <summary>
    /// UrpSceneGray 的 Inspector：提供切换效果按钮。
    /// </summary>
    [CustomEditor(typeof(UrpSceneGray))]
    public class UrpSceneGrayEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var script = target as UrpSceneGray;
            if (script == null)
            {
                return;
            }

            if (GUILayout.Button("切换效果"))
            {
                script.SwitchEff();
                Repaint();
            }
        }
    }
}
```

- [x] **Step 2: Verify file presence and namespaces**

```powershell
Select-String -Path "Runtime/Scripts/SceneGray/*.cs","Editor/Scripts/SceneGray/*.cs" -Pattern "^namespace "
```

Expected: three files, all `namespace ST.Effect`

- [x] **Step 3: Commit**

```bash
git add Editor/Scripts/SceneGray/UrpSceneGrayEditor.cs
git commit -m "feat(SceneGray): add UrpSceneGrayEditor inspector button"
```

---

## Manual verification (Unity)

1. Open a scene with Volume + ColorAdjustments profile.
2. Add `UrpSceneGray` to the Volume GameObject.
3. Click Inspector「切换效果」：画面应变灰 / 还原。
4. Confirm no asmdef changes and no new asset imports in package.
