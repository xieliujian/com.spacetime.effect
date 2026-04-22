// 盒体投影立方体：将世界空间反射方向修正为在盒体 AABB 内与射线求交后的采样方向。
#ifndef ST_BOXCUBE_REFL_UTILS_INCLUDED
#define ST_BOXCUBE_REFL_UTILS_INCLUDED

#include "BoxCubeReflInclude.hlsl"

// 输入：视反射方向、像素世界坐标、探针中心与盒体 min/max。输出：用于 texCUBE 采样的方向向量（世界空间，相对中心偏移后的等效入射点方向）。
half3 BoxProjectedCubemapDirectionCustom(half3 worldRefl, float3 worldPos, float4 cubemapCenter, float4 boxMin, float4 boxMax)
{
    half3 nrdir = normalize(worldRefl);

    // 射线与 AABB 各面求参数 t：沿 +nrdir 方向到 min/max 平面的有符号比
    half3 rbmax = (boxMax.xyz - worldPos) / nrdir;
    half3 rbmin = (boxMin.xyz - worldPos) / nrdir;

    // 根据反射分量正负选取进入盒体前最近的面（t 取小分支）
    half3 boolDir = (nrdir > 0.0f);
    half3 rbminmax = boolDir * rbmax + (1 - boolDir) * rbmin;

    // 三个轴向里最先命中的 t（到盒体可见面的最近交点距离）
    half fa = min(min(rbminmax.x, rbminmax.y), rbminmax.z);

    // 以探针中心为原点的局部位移 + 沿反射方向到盒壁的位移，得到与盒投影一致的方向
    worldPos -= cubemapCenter.xyz;
    worldRefl = worldPos + nrdir * fa;

    return worldRefl;
}

#endif
