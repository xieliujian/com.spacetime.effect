// URP Forward：简单 Unlit 混合 + 世界空间水平面法线反射 + 盒体投影 cubemap 采样。
#ifndef ST_BOXCUBE_REFL_FORWARD_PASS_INCLUDED
#define ST_BOXCUBE_REFL_FORWARD_PASS_INCLUDED

#include "BoxCubeReflUtils.hlsl"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
};

v2f vert (appdata v)
{
    v2f o;

    o.vertex = TransformObjectToHClip(v.vertex);
    o.worldPos = TransformObjectToWorld(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);

    return o;
}

float4 frag (v2f i) : SV_Target
{
    // 主色
    float4 baseCol = tex2D(_MainTex, i.uv);

    float3 worldPos = i.worldPos;
    // 视方向（指向相机）
    half3 viewDirectionWS = (_WorldSpaceCameraPos - worldPos);
    // 水平面 (0,1,0) 上的镜面反射，用于假平面反射；可按需求改为法线贴图/几何法线
    half3 reflectVector = reflect(-viewDirectionWS, float3(0, 1, 0));

    // 视差矫正后的方向，对反射 cubemap 采样
    half3 realReflUV = BoxProjectedCubemapDirectionCustom(reflectVector, worldPos, _BoxCubeReflCenter, _BoxCubeReflBoxMin, _BoxCubeReflBoxMax);

    half4 reflColor = texCUBElod(_ReflectionCube, float4(realReflUV, 0));

    // _BlendPercent 越接近 1 越像主贴图，越小反射越强
    float4 color = baseCol * _BlendPercent + (1 - _BlendPercent) * reflColor;

    return color;
}

#endif
