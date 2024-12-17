Shader "SpaceTime/Scene/BoxCubeRefl"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ReflectionCube("Reflection Cube", Cube) = "" {}

        _BoxCubeReflCenter("_BoxCubeReflCenter", vector) = (0, 0, 0, 0)
        _BoxCubeReflBoxMin("_BoxCubeReflBoxMin", vector) = (-5, -5, -5, 0)
        _BoxCubeReflBoxMax("_BoxCubeReflBoxMax", vector) = (5, 5, 5, 0)

        _BlendPercent("_BlendPercent", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            Name "Unlit"
            Tags {"LightMode" = "UniversalForward"}
            
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BoxCubeReflCenter;
                float4 _BoxCubeReflBoxMin;
                float4 _BoxCubeReflBoxMax;
                float _BlendPercent;
            CBUFFER_END
            
            sampler2D _MainTex;
            samplerCUBE _ReflectionCube;

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
                // sample the texture
                float4 baseCol = tex2D(_MainTex, i.uv);

                float3 worldPos = i.worldPos;
                half3 viewDirectionWS = (_WorldSpaceCameraPos - worldPos);
                half3 reflectVector = reflect(-viewDirectionWS, float3(0, 1, 0));

                half3 realReflUV = BoxProjectedCubemapDirection(reflectVector, worldPos, _BoxCubeReflCenter, _BoxCubeReflBoxMin, _BoxCubeReflBoxMax);

                half4 reflColor = texCUBElod(_ReflectionCube, float4(realReflUV, 0));

                float4 color = baseCol * _BlendPercent + (1 - _BlendPercent) * reflColor;

                return color;
            }

            ENDHLSL
        }
    }
}
