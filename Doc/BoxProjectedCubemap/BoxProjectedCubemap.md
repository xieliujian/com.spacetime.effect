
# 使用Cubemap的盒子投影

## 展示效果

![图标](https://github.com/xieliujian/com.spacetime.effect/blob/main/Doc/BoxProjectedCubemap/Video/BoxProjectedCubemap.png?raw=true)

## 说明

BoxProjectedCubemap适用于矩形的场景，对于矩形场景墙壁上的物件投影效果较好，如上图所示，对于墙壁的门，窗，画能正确投影

## Demo实现

创建一个ReflectionProbe包裹住整个场景，然后拍照生成CubeMap

![图标](https://github.com/xieliujian/com.spacetime.effect/blob/main/Doc/BoxProjectedCubemap/Video/Demo1.png?raw=true)

右键BoxProjectedCubemapDirection_Copy命令复制ReflectionProbe的shader参数，找到反射的物件，右键BoxProjectedCubemapDirection_Paste粘贴，这样就把需要的属性复制到指定的反射材质上

> _BoxCubeReflCenter 盒子投影的中心位置

> _BoxCubeReflBoxMin 盒子投影的最小位置

>  _BoxCubeReflBoxMax 盒子投影的最大位置

假反射面板shader的核心代码

```C#

// 参数
// worldRefl 反射向量
// worldPos 顶点世界空间位置
// cubemapCenter 盒子投影的中心位置
// boxMin 盒子投影的最小位置
// boxMax 盒子投影的最大位置

// 返回值 返回需要的投影坐标， 通过这个函数就能转换成正确的投影坐标

half3 BoxProjectedCubemapDirection(half3 worldRefl, float3 worldPos, float4 cubemapCenter, float4 boxMin, float4 boxMax)
{
    // nrdir对应于我们的d
    half3 nrdir = normalize(worldRefl);

    half3 rbmax = (boxMax.xyz - worldPos) / nrdir;
    half3 rbmin = (boxMin.xyz - worldPos) / nrdir;

    // rbminmax对应t
    //half3 rbminmax = (nrdir > 0.0f) ? rbmax : rbmin;
    half3 boolDir = (nrdir > 0.0f);
    half3 rbminmax = boolDir * rbmax + (1 - boolDir) * rbmin;

    // fa对应collisionDist
    half fa = min(min(rbminmax.x, rbminmax.y), rbminmax.z);

    worldPos -= cubemapCenter.xyz;

    // 下面的 worldPos 对应 localPosInProbe 
    // nrdir * fa 等于 collisionDir
    worldRefl = worldPos + nrdir * fa;

    return worldRefl;
}

```

## 原理

网页的第三章节原理介绍

[catlikecoding](https://catlikecoding.com/unity/tutorials/rendering/part-8/)


