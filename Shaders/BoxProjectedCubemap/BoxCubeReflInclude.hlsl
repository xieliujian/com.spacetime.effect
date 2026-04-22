// 盒体反射：材质 CBUFFER 与贴图采样器声明。由 BoxCubeReflUtils / ForwardPass 链式引用。
#ifndef ST_BOXCUBE_REFL_INCLUDE_INCLUDED
#define ST_BOXCUBE_REFL_INCLUDE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

// 与 Shader Properties 一一对应，由 SRP Batcher / 材质实例填充
CBUFFER_START(UnityPerMaterial)
    // _MainTex 的 Tiling/Offset
    float4 _MainTex_ST;
    // 盒体中心（与反射探针包围盒一致）
    float4 _BoxCubeReflCenter;
    float4 _BoxCubeReflBoxMin;
    float4 _BoxCubeReflBoxMax;
    // 主贴图与反射立方体结果的混合权重
    float _BlendPercent;
CBUFFER_END

sampler2D _MainTex;
samplerCUBE _ReflectionCube;

#endif
