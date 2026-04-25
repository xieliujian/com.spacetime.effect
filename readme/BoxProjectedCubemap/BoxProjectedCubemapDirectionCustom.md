# `BoxProjectedCubemapDirectionCustom` 原理分析

[← 返回 盒子投影 Cubemap](BoxProjectedCubemap.md) · [← 返回主页](../../README.md)

---

## 先说问题：为什么反射会「错位」

反射探针把房间的样貌**从探针中心**拍成一张 Cubemap。

采样时，GPU 需要一个**方向向量**，去 Cubemap 里查「这个方向上看到什么」。最直接的做法是直接用**镜面反射方向**（`reflect(视线, 法线)`）去查。

这在环境很远时没问题——远处的山、天空，站在房间哪个角度看反射都差不多。  
但在**室内近距离**场景（地板、墙面）就会出错：

```
你的位置（★） 和 探针中心（●） 不同

★                   ●
│                   │
│ ← 你的反射方向    │ ← 探针拍图时的方向
│                   │
└── 地板 ───────────┘

两个方向一样，但你在左边，探针在中间。
你看到的反射应该偏右，但 Cubemap 给的是"中间视角"。
```

结果就是：**无论你走到房间哪里，地板反射永远像同一张贴图，不会随位置变化**。

---

## 修正思路：改用「交点方向」

真实世界里，镜子反射的原理是：

> 你的视线沿反射方向打出去，**打到了哪面墙**，你就看到那面墙上探针拍到的图像。

所以正确的采样方向不是「反射方向」本身，而是：

> **从探针中心，指向「反射光线打到的那面墙」的方向**

```
★ = 你的位置     ● = 探针中心     × = 反射光线打到的墙

        ×
       /  ← 正确采样方向（● → ×）
      /
     ★
      \
       ← 错误采样方向（直接用反射方向，像从 ● 出发）
```

`BoxProjectedCubemapDirectionCustom` 做的事就是：

1. 从当前像素出发，沿反射方向，**找到它打到盒子哪一面墙**
2. 把「探针中心 → 交点」这个向量**交给 Cubemap 采样**

---

## 核心数学：射线打到哪面墙？

### 第一步：给射线写方程

从像素出发，沿反射方向走一段距离 `t`，到达的点是：

```
命中点 = worldPos + t × nrdir
```

其中 `nrdir` 是归一化的反射方向。`t` 是我们要求的「走了多远」。

### 第二步：对每个轴算到达各面墙的 `t`

房间用 AABB 盒子表示，有 6 面墙（x/y/z 各两面）。  
对 **x 轴**来说，有 `boxMin.x` 和 `boxMax.x` 两面墙：

```
命中点.x = worldPos.x + t × nrdir.x = boxMin.x 或 boxMax.x

解出 t：
    t_xMin = (boxMin.x - worldPos.x) / nrdir.x
    t_xMax = (boxMax.x - worldPos.x) / nrdir.x
```

这两行对应代码中的：

```hlsl
half3 rbmin = (boxMin.xyz - worldPos) / nrdir;   // 三轴各自到 min 面的 t
half3 rbmax = (boxMax.xyz - worldPos) / nrdir;   // 三轴各自到 max 面的 t
```

此时我们有 6 个候选 t 值（x/y/z × min/max）。

### 第三步：排除走反方向的面

光线沿 `nrdir` 方向走，`t > 0` 才是前方的墙。  
但不需要显式判断 `t > 0`，只需根据方向符号**选对那面墙**：

- 若 `nrdir.x > 0`（往正 x 走），前方是 `boxMax.x`，用 `t_xMax`
- 若 `nrdir.x < 0`（往负 x 走），前方是 `boxMin.x`，用 `t_xMin`

```hlsl
half3 boolDir  = (nrdir > 0.0f);                        // 分量 > 0 则为 1，否则为 0
half3 rbminmax = boolDir * rbmax + (1 - boolDir) * rbmin; // 正方向选 max，负方向选 min
```

每一轴都选出了「前方那面墙」对应的 `t`，共得到 3 个候选值（x、y、z 各一个）。

### 第四步：取最小的 `t`——最先碰到的墙

射线先到哪面墙，就取哪面墙。三个候选 `t` 里最小的，对应最先命中的那面：

```hlsl
half fa = min(min(rbminmax.x, rbminmax.y), rbminmax.z);
```

**例子**——假设你站在房间中偏左下角，往右上方向照射：

```
t_x = 3.0   （要走 3 才到右墙）
t_y = 1.5   （要走 1.5 才到天花板）  ← 最小，先打到天花板
t_z = 5.0   （要走 5 才到前墙）

fa = 1.5
```

### 第五步：算「探针中心 → 交点」的向量

交点的世界坐标：

```
交点 = worldPos + nrdir × fa
```

我们要的是从**探针中心指向交点**的向量：

```
采样方向 = 交点 - cubemapCenter
         = (worldPos - cubemapCenter) + nrdir × fa
```

代码把这两步合在一起写：

```hlsl
worldPos -= cubemapCenter.xyz;       // worldPos 变成"相对探针中心的偏移"
worldRefl = worldPos + nrdir * fa;   // 偏移 + 沿反射方向走到墙，即为采样向量
```

---

## 完整代码逐行注释

```hlsl
half3 BoxProjectedCubemapDirectionCustom(
    half3  worldRefl,       // 表面的镜面反射方向（未归一化）
    float3 worldPos,        // 当前像素的世界坐标
    float4 cubemapCenter,   // 反射探针中心（世界坐标）
    float4 boxMin,          // AABB 最小角（世界坐标）
    float4 boxMax)          // AABB 最大角（世界坐标）
{
    half3 nrdir = normalize(worldRefl);
    // 归一化反射方向，确保后面 t 的计算单位一致

    half3 rbmax = (boxMax.xyz - worldPos) / nrdir;
    half3 rbmin = (boxMin.xyz - worldPos) / nrdir;
    // 每个轴：从 worldPos 到 max/min 面，需要走多少步 t
    // 正数 = 前方，负数 = 背后（已被下一步过滤）

    half3 boolDir  = (nrdir > 0.0f);
    half3 rbminmax = boolDir * rbmax + (1 - boolDir) * rbmin;
    // 按方向选「前方那面墙」：
    //   往正方向走 → 选 max 面
    //   往负方向走 → 选 min 面

    half fa = min(min(rbminmax.x, rbminmax.y), rbminmax.z);
    // 三轴里最先到达的那面墙，对应最小的 t

    worldPos -= cubemapCenter.xyz;
    worldRefl = worldPos + nrdir * fa;
    // 采样向量 = 从探针中心指向交点
    //          = (worldPos - center) + nrdir × fa

    return worldRefl;
}
```

---

## 用一张图串联全过程

```
世界坐标系俯视图（Y 轴朝上，看 XZ 平面）

       boxMin.z ──────────────── boxMax.z
          │                          │
boxMin.x  │       ●（探针中心）       │  boxMax.x
          │                          │
          │         ★（像素位置）     │
          │          ↗ nrdir          │
          │         /                │
          │        /                 │
          └───────×──────────────────┘
                  ↑ 交点（射线打到的那面墙）

采样方向 = ● → × = (★ - ●) + nrdir × fa
```

探针中心 `●` 拍到的 Cubemap 里，`× 方向`对应的像素，就是这个像素该看到的反射内容。

---

## 与「直接用反射方向」的对比

| | 直接用反射方向 | 盒投影修正 |
|---|---|---|
| 采样向量起点 | 隐含为探针中心 | 显式算出，考虑了像素位置 |
| 采样向量 | `nrdir`（固定） | `(worldPos - center) + nrdir × fa`（随位置变化） |
| 效果 | 像贴纸，位置不变 | 随观察位置变化，反射正确跟移 |
| 适用场景 | 天空、远山等 | 室内地板、墙面、近处物体 |

---

## 分母为零的情况

当 `nrdir` 某一轴分量为 0（反射方向平行于该轴的两面墙），除法会产生 `±Inf`。  
`±Inf` 不会是 `min` 的胜者（`min(1.5, +Inf) = 1.5`），所以**被自动排除**，不影响最终结果。  
这是 HLSL 浮点数规范保障的行为，无需额外处理。

---

## 局限

- 只适合 **轴对齐的矩形空间**；弧形墙、圆柱形房间需要其他交叉测试。
- **单探针**只能覆盖一个盒子；大场景需要多个探针混合过渡。
- 探针盒范围应**紧密包裹**反射可见区域，若反射面明显超出盒子边界，修正会失效。

---

[← 返回 盒子投影 Cubemap](BoxProjectedCubemap.md) · [← 返回主页](../../README.md)
