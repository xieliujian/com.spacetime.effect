# `BoxProjectedCubemapDirectionCustom` 原理分析

[← 返回 盒子投影 Cubemap](BoxProjectedCubemap.md) · [← 返回主页](../../README.md)

---

## 一文读懂：它到底在修什么

想象你在**方形房间里**看一面**光滑的地板**或墙面：真实世界里，你站在不同位置、看向镜面时，**反射里的柱子、门洞位置会跟着变**。

但 GPU 用 **反射探针 + Cubemap** 时，图像是**从探针中心**「拍」出来的：若采样时**只用镜面反射方向**、不管你现在站在房间哪里，大家看到的反射会像**同一台摄像机拍的壁纸**——[Catlike Coding《Rendering 8》](https://catlikecoding.com/unity/tutorials/rendering/part-8/) 里说的：**对无穷远环境可以近似，对近处墙面、地板的反射会错位**。

**盒投影（Box Projection）** 就是一招便宜修正：**用与房间对齐的 AABB 盒子**，在算 Cubemap 采样方向时，假装「从当前像素沿反射线先碰到哪一面墙」，用这一几何关系去**改**传给 `texCUBE` 的向量。  
`BoxProjectedCubemapDirectionCustom` 做的事，与教程里自写的 `BoxProjection`、以及 Unity 内置的 `BoxProjectedCubemapDirection` **数学上一类**，只是用 **中心 + min/max** 表达，便于和本项目的 `ReflectionProbe` 数据对齐。

实现代码：[`BoxCubeReflUtils.hlsl`](../../Shaders/BoxProjectedCubemap/BoxCubeReflUtils.hlsl)。

---

## 从《Rendering 8》看三条递进关系

| 阶段 | 教程在讲什么 | 和我们函数的关系 |
|------|----------------|------------------|
| 环境映射 | 用 3D **方向** 去 Cubemap 里取色；有 HDR / mip 等细节 | 我们最终产出的就是「该往哪指」的向量，再交给 `texCUBElod` |
| 反射不是画皮肤 | 要用 **`reflect(视线, 法线)`** 当方向，而不是用普通法线当方向 | 方向由 **调用方** 传入（如 `BoxCubeReflForwardPass`）；盒投影**不管**法线从哪来 |
| Box Projection | 在**与 Cubemap 对齐的矩形空间**里，用射线与**盒面**的交点来改采样方向 | 本函数 = 这段几何在工程里的一个实现变体 |

教程原文要点（意译）：

- 只放一个探针时，**所有球体反射像站在探针中心**，地板镜子则 **位置、尺度都不对**。
- 若环境**很远**，可以当作无穷远，不必在意观察点；**墙、地板离得近**就要管观察点。
- 空立方体房间里，从任意表面点、沿反射方向，射线会与**某一面墙/顶/地**先相交；用 **从房间中心指向该交点** 的向量去采样 Cubemap，就能对齐。

> 更完整的叙事与动图见：[Rendering 8 — Box Projection](https://catlikecoding.com/unity/tutorials/rendering/part-8/) 中 **《Box Projection》** 整节（含 *Reflection Probe Box*、*Adjusting the Sample Direction*）。

---

## 直观模型：三句话

1. 已知：**当前像素世界坐标** `worldPos`、**归一化反射方向** `nrdir`、**盒子**的 `min/max` 与**探针中心** `cubemapCenter`（与 [ReflectionProbe 的盒范围](https://catlikecoding.com/unity/tutorials/rendering/part-8/) 同构）。
2. 沿 `nrdir` 从 `worldPos` 射出一条射线，在 **x、y、z 三个方向** 上分别算「若只沿这个方向走，**最先撞到 max 面还是 min 面**」对应的**参数 t**；三个 t 里取**最小**的那个 `fa`（**最先**碰到的就是盒子的**那堵墙**）。
3. 把 `worldPos` 挪到**以 `cubemapCenter` 为原点**，再加上沿反射方向走 `fa` 的位移，得到一个新向量，用作 Cubemap 采样方向——这样反射才跟「人站在盒子里」一致。

**比喻**：在走廊里用激光笔照天花板，**先打到哪块板**由三个方向里**最短有效路程**决定；`fa` 就是这段路程。教程里也强调：**选最小的标量，对应最近的那张边界**（*which bounds face is closest*）。

---

## 和 Catlike 教程公式的同一件事

教程里在归一化方向之后，写成（逻辑等价于下面三行；变量名与 Unity 原稿一致）：

```hlsl
float3 factors = ((direction > 0 ? boxMax : boxMin) - position) / direction;
float scalar = min(min(factors.x, factors.y), factors.z);
return direction * scalar + (position - cubemapPosition);
```

本项目的写法（`boolDir` 与 `rbmax`/`rbmin` 的混合）是**同一公式的分步**：

- `rbmax` / `rbmin` 对应 `boxMax` / `boxMin` 与 `worldPos` 在 **各轴上** 除 `nrdir` 得到的候选 t；
- `boolDir` 在每一轴上选「沿正方向用 max 界、负方向用 min 界」——与三目 `direction > 0 ? boxMax : boxMin` 相同；
- `fa` = 三个轴候选 t 的 **min**，即教程里的 `scalar`；
- 最后 `worldPos -= cubemapCenter` 再 `+ nrdir * fa`，与 `direction * scalar + (position - cubemapPosition)` **代数一致**（`direction` 与 `nrdir` 在实现里会先做 `normalize`）。

教程还说明：**Cubemap 采样不必强制归一化方向**，因为硬件本质也是**按方向找面再插值**；我们仍对反射方向做 `normalize`，与常见 URP/内置写法一致。参见教程 *Doesn't the new direction have to be normalized?* 一节。

#### 分母为零怎么办？

与教程 *What happens when one of the divisions is by zero?* 一致：某一轴分量为 0 时，该轴除法会出问题，但 **不会进入 `min` 的「胜出」结果**（无效值在比较里被排掉），工程上可接受；若你移植到**任意** `worldPos`，更稳妥的做法是加极小 ε，本仓库保持与原始算法一致。

---

## 与主文档、示例 Shader 的衔接

- 工具流、**`_BoxCubeReflCenter` 等** 与 Copy/Paste：见 [盒子投影 Cubemap](BoxProjectedCubemap.md)。  
- 片元里**固定水平法线**等简化：见 [`BoxCubeReflForwardPass.hlsl`](../../Shaders/BoxProjectedCubemap/BoxCubeReflForwardPass.hlsl)；**盒投影**与**法线从哪来**是两层问题，可替换为法线贴图/几何法线。  
- 教程提醒：**反射面不要超出探针盒太多**，否则盒投影的近似会破功——布置探针时把 **Box 调到包住可见反射区域** 仍是前提。

---

## 局限（教程也强调）

- 最适合 **与轴对齐的矩形房间**；圆柱、曲面墙需要别的表示。  
- **单一探针** 只能在一个盒近似下正确；大场景要 **多探针混合**（教程后半 *Blending Reflection Probes*），那是另一条管线，不在这个函数里。  
- 探针**从某高度拍地板**，地板会映出**不该出现的一块地板**等——要调 **探针原点在盒内的位置** 折中，见 [Rendering 8](https://catlikecoding.com/unity/tutorials/rendering/part-8/)  *Lowered probe center* 等图。

---

## 延伸阅读

- **主教程**（从天空盒、反射到盒投影的完整线）：[Catlike Coding — Unity Rendering, Part 8, Reflections](https://catlikecoding.com/unity/tutorials/rendering/part-8/)  
- 本包流程与 API：[盒子投影 Cubemap](BoxProjectedCubemap.md)

---

[← 返回 盒子投影 Cubemap](BoxProjectedCubemap.md) · [← 返回主页](../../README.md)
