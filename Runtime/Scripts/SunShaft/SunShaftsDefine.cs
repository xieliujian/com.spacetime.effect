using UnityEngine;

namespace ST.Effect.URP
{
    /// <summary>
    /// SunShaft 效果的常量定义，集中管理 Shader 路径名、属性 ID、CommandBuffer 名称与可见性裁剪角度，
    /// 供 SunShaftsFeatureV2、SunShaftsProperties、SunShaftsPass 共用，避免散落的魔法字符串与重复定义。
    /// </summary>
    public static class SunShaftsDefine
    {
        // ──────────────────────────────────────────
        // Shader 路径名
        // ──────────────────────────────────────────

        /// <summary>Pass 1：天空区域采样 + 噪声 Shader 路径。</summary>
        public static readonly string s_BuildSkyShaderName = "SpaceTime/PostProcess/SunShaft/BuildSkyForBlurShader";

        /// <summary>Pass 2：径向模糊 Shader 路径。</summary>
        public static readonly string s_BlurShaderName = "SpaceTime/PostProcess/SunShaft/DirectionalBlurShader";

        /// <summary>Pass 3：强度/颜色/遮罩混合回场景 Shader 路径。</summary>
        public static readonly string s_FinalBlendShaderName = "SpaceTime/PostProcess/SunShaft/FinalBlendShader";

        // ──────────────────────────────────────────
        // CommandBuffer 名称
        // ──────────────────────────────────────────

        /// <summary>CommandBuffer 标识名，用于性能分析面板中的渲染阶段显示。</summary>
        public static readonly string s_CommandBufferName = "ShaftsRendering";

        // ──────────────────────────────────────────
        // 可见性裁剪角度
        // ──────────────────────────────────────────

        /// <summary>摄像机前向与主光源方向夹角上限（度）；超出则跳过渲染。</summary>
        public static readonly float s_CanVisibleRenderLightAngle = 30.0f;

        /// <summary>摄像机前向与世界 Up 向量夹角上限（度）；超出则跳过渲染。</summary>
        public static readonly float s_CanVisibleRenderUpAngle = 70.0f;

        // ──────────────────────────────────────────
        // Shader 属性 ID
        // ──────────────────────────────────────────

        /// <summary>太阳在屏幕上的归一化坐标（Viewport），传递给 BuildSky / Blur Pass。</summary>
        public static readonly int s_Shader_SunPosition_PropId = Shader.PropertyToID("_SunPosition");

        /// <summary>径向模糊步长（归一化屏幕空间），每次迭代动态计算后写入。</summary>
        public static readonly int s_Shader_BlurStep_PropId = Shader.PropertyToID("_BlurStep");

        /// <summary>光轴强度，传递给 FinalBlend Pass。</summary>
        public static readonly int s_Shader_Intensity_PropId = Shader.PropertyToID("_Intensity");

        /// <summary>光轴颜色（HDR），传递给 FinalBlend Pass。</summary>
        public static readonly int s_Shader_ShaftsColor_PropId = Shader.PropertyToID("_ShaftsColor");

        /// <summary>天空采样亮度阈值，低于此值的像素不计入光轴。</summary>
        public static readonly int s_Shader_SunThresholdSky_PropId = Shader.PropertyToID("_SunThresholdSky");

        /// <summary>天空噪声缩放系数，控制光轴噪声纹理的采样频率。</summary>
        public static readonly int s_Shader_SkyNoiseScale_PropId = Shader.PropertyToID("_SkyNoiseScale");

        /// <summary>是否启用模板遮罩纹理（0 = 关闭，1 = 开启），传递给 FinalBlend Pass。</summary>
        public static readonly int s_Shader_UseStencilMaskTex_PropId = Shader.PropertyToID("_UseStencilMaskTex");

        /// <summary>模板遮罩贴图属性 ID，通过 SetGlobalTexture 绑定到全局 Shader 属性。</summary>
        public static readonly int s_Shader_StencilMaskTex_PropId = Shader.PropertyToID("_StencilMaskTex");
    }
}
