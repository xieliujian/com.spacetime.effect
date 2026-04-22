// BoxProjectedCubemap：使用盒体（AABB）矫正的反射立方体采样，用于平面/假反射等场景。
// 依赖 URP Forward，顶点/片元实现见 BoxCubeReflForwardPass.hlsl。
Shader "SpaceTime/Scene/BoxCubeRefl"
{
    Properties
    {
        // 主贴图
        _MainTex ("Texture", 2D) = "white" {}
        // 环境/反射立方体贴图
        _ReflectionCube("Reflection Cube", Cube) = "" {}

        // 盒体投影：探针/包围盒中心（世界空间）
        _BoxCubeReflCenter("_BoxCubeReflCenter", vector) = (0, 0, 0, 0)
        // 盒体最小角点（世界空间）
        _BoxCubeReflBoxMin("_BoxCubeReflBoxMin", vector) = (-5, -5, -5, 0)
        // 盒体最大角点（世界空间）
        _BoxCubeReflBoxMax("_BoxCubeReflBoxMax", vector) = (5, 5, 5, 0)

        // 基础色与反射的混合：越大越接近主贴图颜色
        _BlendPercent("_BlendPercent", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Name "Unlit"
            // URP 前向主光 Pass
            Tags {"LightMode" = "UniversalForward"}
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            // 声明雾效变体（若片元中启用雾需搭配对应宏）
            #pragma multi_compile_fog

            // URP 核心 / 部分输入与光照数据（如相机位置）
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            // 顶点、片元与材质依赖
            #include "BoxCubeReflForwardPass.hlsl"

            ENDHLSL
        }
    }
}
