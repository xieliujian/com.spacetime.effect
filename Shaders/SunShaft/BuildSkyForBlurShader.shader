Shader "SpaceTime/PostProcess/SunShaft/BuildSkyForBlurShader"
{
    Properties
    {
         [NoScaleOffset] _MainTex("Main Texture", 2D) = "black" {}
        _SunPosition("SunPosition", Vector) = (0.5, 0.5, 0, 0)
        _SunThresholdSky("SunThresholdSky", Float) = 0.99
        _SkyNoiseScale("Sky Noise Scale", Float) = 250
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #define REQUIRE_DEPTH_TEXTURE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "SimpleNoise.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv : TEXCOORD0;
                float4 ScreenPosition : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                float2 _SunPosition;
                float _SunThresholdSky;
                float _SkyNoiseScale;
            CBUFFER_END

            // Object and Global properties
            SAMPLER(SamplerState_Linear_Repeat);
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                float3 PosOS = input.positionOS.xyz;
                float3 positionWS = TransformObjectToWorld(PosOS);
                float4 positionCS = TransformWorldToHClip(positionWS);

                output.positionCS = positionCS;
                output.uv = input.uv;
                output.ScreenPosition = ComputeScreenPos(positionCS, _ProjectionParams.x);

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 uv = input.uv;
                float4 screenPos = input.ScreenPosition;

                // Distance from Sun
                float disFromSun = length(_SunPosition.xy - uv.xy);

                // Limit for SkyBox by Sun Distance
                float limitSkyBySunDis = saturate(_SunThresholdSky - disFromSun);

                // 
                float sceneDepth = Linear01Depth(SHADERGRAPH_SAMPLE_SCENE_DEPTH(screenPos.xy / screenPos.w), _ZBufferParams);
                float sceneDepthComp = (sceneDepth >= 0.99) ? 1 : 0;

                limitSkyBySunDis *= sceneDepthComp;

                // 
                float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv.xy);
                float noiseVal;
                Unity_SimpleNoise_float(uv.xy, _SkyNoiseScale, noiseVal);
                mainTexColor *= noiseVal;

                float4 color = mainTexColor * limitSkyBySunDis;

                return color;
            }

            ENDHLSL
        }
    }
}
