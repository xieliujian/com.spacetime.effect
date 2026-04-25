# SunShaft 实现原理

[← 返回 SunShaft 光线](SunShaft.md) · [← 返回主页](../../README.md)

---

## 算法概览

SunShaft（太阳光轴/体积光）的屏幕空间实现思路来自经典的 **Image-Space God Rays**：不追踪真实光路，而是在已渲染的帧缓冲和深度缓冲上做三步后处理，用廉价的 2D 操作模拟体积散射：

```
Pass 1 — 提取光源附近的天空像素（加噪声扰动）
Pass 2 — 以光源为中心做指数间距径向模糊（多次迭代累积）
Pass 3 — 强度 × 颜色 × 遮罩，叠加回场景
```

三步全在屏幕空间完成，无需深度重建、无需 3D 光线步进，GPU 开销低且易于接入 URP 后处理管线。

---

## Pass 1 — 天空提取（`BuildSkyForBlurShader`）

### 目标

从当前帧找出「靠近太阳、且属于天空（未被几何遮挡）」的像素，作为光源种子，再叠加噪声使其不均匀。

### 深度剔除：只留天空

```hlsl
float sceneDepth = Linear01Depth(
    SHADERGRAPH_SAMPLE_SCENE_DEPTH(screenPos.xy / screenPos.w),
    _ZBufferParams);
float sceneDepthComp = (sceneDepth >= 0.99) ? 1 : 0;
```

深度缓冲中，天空盒永远写入远截面（`depth ≈ 1.0`）。阈值 `0.99` 区分「几何体」与「天空」——只有天空像素的 `sceneDepthComp = 1`，其余归零。

### 距离遮罩：只保留太阳周围

```hlsl
float disFromSun = length(_SunPosition.xy - uv.xy);
float limitSkyBySunDis = saturate(_SunThresholdSky - disFromSun);
```

`_SunPosition` 是太阳在归一化视口坐标（Viewport Space，0–1）中的位置，由 C# 侧通过 `Camera.WorldToViewportPoint` 计算后传入。

`saturate(_SunThresholdSky - disFromSun)` 构建了一个**以太阳为中心的径向渐变遮罩**：

| 位置 | `disFromSun` | 遮罩值 |
|------|--------------|--------|
| 正好在太阳处 | 0 | `_SunThresholdSky`（最大） |
| 距离 = `_SunThresholdSky` 处 | `_SunThresholdSky` | 0（边界） |
| 更远处 | > `_SunThresholdSky` | 0（完全剔除） |

将深度遮罩与距离遮罩相乘，得到「天空中靠近太阳」的区域：

```hlsl
limitSkyBySunDis *= sceneDepthComp;
```

### 噪声扰动：破坏均匀感

```hlsl
float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv.xy);
float noiseVal;
Unity_SimpleNoise_float(uv.xy, _SkyNoiseScale, noiseVal);
mainTexColor *= noiseVal;
```

将场景颜色先乘以噪声，再乘以遮罩。如果直接用均匀亮度的圆形区域做后续模糊，光轴会是「完美的白晕」；叠加噪声后颜色分布不均，模糊后自然产生**明暗交替的条纹感**，更接近真实丁达尔效应。

噪声函数 `Unity_SimpleNoise_float` 的实现见下文「[SimpleNoise 三倍频值噪声](#simplenoise-三倍频值噪声)」章节。

### Pass 1 输出

```hlsl
return mainTexColor * limitSkyBySunDis;
```

最终输出：只有天空中、距太阳 `_SunThresholdSky` 范围内的像素被保留，其余为黑色。

---

## Pass 2 — 径向模糊（`DirectionalBlurShader`）

### 目标

从每个像素出发，沿「**指向太阳**」方向，以指数间距采样 6 次，叠加后取均值，制造向太阳方向延伸的光轴拖影。

### 指数间距采样

```hlsl
float2 uvOffset = (_SunPosition.xy - uv.xy) * _BlurStep;
```

`uvOffset` 的**方向**指向太阳，**大小**由 `_BlurStep` 控制。6 次采样的位移距离依次为基础偏移的 **0、1、2、4、8、16 倍**：

| 采样 | UV 偏移 | 相对倍数 |
|------|---------|---------|
| c0 | `uv` | 0× |
| c1 | `uv + uvOffset` | 1× |
| c2 | `uv + 2 × uvOffset` | 2× |
| c3 | `uv + 4 × uvOffset` | 4× |
| c4 | `uv + 8 × uvOffset` | 8× |
| c5 | `uv + 16 × uvOffset` | 16× |

这种**指数间距**（1、2、4、8、16）的含义：

- **近处密**：c0–c2 采样间距小，保留光轴根部的细节
- **远处稀**：c3–c5 采样间距大，用少量采样覆盖更长的光轴拖尾

仅用 6 次采样就能模拟出从近到远拉伸的光轴效果，是一种高效的近似。

> 若用均匀间距（0、1、2、3、4、5×），光轴长度有限，想延伸须增加采样数（更费）；指数间距以极少采样点覆盖更大范围。

### 累加求均值

Shader 内把 6 个采样直接相加再除以 6，但由于乘加树的具体写法，c2 和 c3 被累加了两次，实际权重为：

```
(c0 + c1 + 2·c2 + 2·c3 + c4 + c5) / 6
```

中段采样（距太阳中等距离）权重略高，使光轴在中段亮度更饱满，近端和远端稍弱——这是实现带来的轻微加权效果，无需刻意修正。

### 多次迭代累积（C# 侧）

单次 Pass 2 只能覆盖固定的 `_BlurStep` 距离。为产生更长、更柔和的光轴，C# 驱动层（`SunShaftsPass.Execute`）以**递增的模糊步长**执行多轮 Pass 2，在两个临时纹理 `TmpBlurTex1 ↔ TmpBlurTex2` 之间交替写入：

```csharp
// 每轮两次 Blit，步长成倍增长
var radius = blurRadius / radiusDivider;                       // 初始步长
var iterationScaler = shaderBlurIterationsCount / radiusDivider;  // = 6 / 750

for (int i = 0; i < blurStepsCount; i++)
{
    // 正向：TmpBlurTex1 → TmpBlurTex2
    blurMaterial.SetFloat(_BlurStep, radius);
    Blit(TmpBlurTex1 → TmpBlurTex2);

    // 步长增大
    radius = blurRadius * (i * 2f + 1f) * iterationScaler;

    // 反向：TmpBlurTex2 → TmpBlurTex1
    blurMaterial.SetFloat(_BlurStep, radius);
    Blit(TmpBlurTex2 → TmpBlurTex1);

    // 步长继续增大
    radius = blurRadius * (i * 2f + 2f) * iterationScaler;
}
```

每迭代一次，光轴被进一步拉长，最终 `TmpBlurTex1` 中存储累积的多级模糊结果。

### 降采样与性能

Pass 2 的临时纹理在分辨率上做了**位移降采样**：

```csharp
var dstWidth  = cameraWidth  >> depthDownscalePow2;
var dstHeight = cameraHeight >> depthDownscalePow2;
```

`depthDownscalePow2 = n` 意味着纹理宽高缩小为 `1 / 2^n`（像素数缩小为 `1 / 4^n`）。n = 3 时降为 1/8 分辨率，模糊开销大幅降低，最终叠加时双线性放大几乎看不出损失。

---

## Pass 3 — 与场景混合（`FinalBlendShader`）

### 强度与颜色

```hlsl
float4 shaftTexColor = SAMPLE_TEXTURE2D(_TmpBlurTex1, sampler_TmpBlurTex1, uv.xy);
shaftTexColor *= _Intensity;
shaftTexColor = saturate(shaftTexColor) * _ShaftsColor;
```

先乘强度再 `saturate`，防止 HDR 超出后直接溢出；之后乘光轴颜色（C# 侧会根据 `useSunLightColor` 决定是否用主方向光颜色填入 `_ShaftsColor`）。

### 模板遮罩（可选）

```hlsl
float4 maskTexColor = SAMPLE_TEXTURE2D(_StencilMaskTex, sampler_StencilMaskTex, uv.xy);
float maskVal = saturate(1 - saturate(maskTexColor));
maskVal = _UseStencilMaskTex * maskVal + (1 - _UseStencilMaskTex);
shaftTexColor *= maskVal;
```

遮罩逻辑是**反向遮罩**：`_StencilMaskTex` 中越**亮**的区域，`maskVal` 越小，光轴越弱；`_StencilMaskTex` 为黑色时 `maskVal = 1`，光轴完全保留。

`_UseStencilMaskTex` 为 0 时，表达式退化为：

```
maskVal = 0 × maskVal + 1 = 1
```

相当于遮罩被旁路，不影响光轴。

### 加法叠加

```hlsl
float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv.xy);
return mainTexColor + shaftTexColor;
```

光轴以**纯加法**叠加在场景颜色上，这正确模拟了大气散射增加亮度的物理行为，不会压暗场景。

---

## SimpleNoise — 三倍频值噪声

`SimpleNoise.hlsl` 实现了 3 倍频的**值噪声（Value Noise）叠加**，用于给 Pass 1 的天空种子增加自然扰动。

### 随机值生成

```hlsl
float unity_noise_randomValue(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
}
```

经典 hash 函数，对整数格点坐标产生伪随机 [0, 1] 值。

### 单层值噪声

```hlsl
float unity_valueNoise(float2 uv)
{
    float2 i = floor(uv);  // 格点坐标
    float2 f = frac(uv);   // 单元内偏移
    f = f * f * (3.0 - 2.0 * f);  // Hermite 平滑插值（smoothstep 曲线）

    // 四个格角随机值
    float r0 = unity_noise_randomValue(i + float2(0,0));
    float r1 = unity_noise_randomValue(i + float2(1,0));
    float r2 = unity_noise_randomValue(i + float2(0,1));
    float r3 = unity_noise_randomValue(i + float2(1,1));

    // 双线性插值
    float bottom = lerp(r0, r1, f.x);
    float top    = lerp(r2, r3, f.x);
    return lerp(bottom, top, f.y);
}
```

`f = f*f*(3-2*f)` 是 **Hermite 三次插值**（与 `smoothstep` 等价），使格点间过渡平滑、无明显方格感。

### 三倍频叠加

```hlsl
void Unity_SimpleNoise_float(float2 UV, float Scale, out float Out)
{
    float t = 0.0;

    // 倍频 0：频率 1，振幅 0.125
    t += unity_valueNoise(UV * Scale / 1.0) * 0.125;

    // 倍频 1：频率 2，振幅 0.25
    t += unity_valueNoise(UV * Scale / 2.0) * 0.25;

    // 倍频 2：频率 4，振幅 0.5
    t += unity_valueNoise(UV * Scale / 4.0) * 0.5;

    Out = t;
}
```

> 注意振幅分配与标准 fBm（高频低振幅）**相反**：高频倍频振幅更大（0.5 > 0.25 > 0.125），高频细节主导，噪声偏细腻，最大输出约 **0.875**（非归一化到 1）。

将此噪声乘到天空颜色上，光轴种子会出现高频的明暗斑点，经过 Pass 2 的径向模糊后产生**不均匀的光柱条纹**，避免了完美圆晕的人工感。

---

## 各参数与公式的对应关系

| 参数 | 作用位置 | 公式影响 |
|------|---------|---------|
| `_SunThresholdSky` | Pass 1 距离遮罩 | 控制圆形遮罩半径，越大光源种子覆盖越广 |
| `_SkyNoiseScale` | Pass 1 噪声 | 值越大噪声越细（高频），光柱纹理越细碎 |
| `depthDownscalePow2` | Pass 2 纹理尺寸 | 2^n 倍缩小，n 越大越快但越模糊 |
| `blurRadius` | Pass 2 `_BlurStep` 初始值 | 控制单次模糊拉伸距离 |
| `blurStepsCount` | Pass 2 迭代次数 | 每多一次迭代，光柱再延伸一倍 |
| `_Intensity` | Pass 3 强度 | 线性放大光轴亮度后再 saturate |
| `_ShaftsColor` | Pass 3 颜色 | 乘法染色，可用主方向光颜色自动同步 |
| `_UseStencilMaskTex` | Pass 3 遮罩开关 | 0 = 旁路，1 = 启用反向遮罩 |

---

## 局限与常见问题

- **光轴颜色取自场景颜色**，Pass 1 不做亮度阈值，场景中其他接近 depth=1 的薄物体（极远处几何体）可能被误提取。
- **深度阈值 0.99 是固定的**，不可调；若项目开启了某些深度压缩，远处几何体深度可能也接近 1.0，导致误提取。
- **指数采样在 `_BlurStep` 较大时会跳格**，UV 偏移 16× 时可能采到画面边缘以外（纹理会 Clamp 或 Repeat），此时远端样本失真；降低 `blurRadius` 或增加迭代次数代替增大单次步长。
- **噪声振幅未归一化**（最大 0.875），在极暗的天空场景下，噪声调制后输出偏低，光轴可能较暗；可适当降低 `_SkyNoiseScale` 使噪声粒度变粗、振幅分布更均匀。

---

[← 返回 SunShaft 光线](SunShaft.md) · [← 返回主页](../../README.md)
