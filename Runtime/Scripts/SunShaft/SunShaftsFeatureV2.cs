
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ST.Effect.URP
{
    /// <summary>
    /// 
    /// </summary>
    public class SunShaftsFeatureV2 : ScriptableRendererFeature
    {
        /// <summary>
        /// 
        /// </summary>
        public static readonly string BUILD_SKY_SHADER_NAME = "SpaceTime/PostProcess/SunShaft/BuildSkyForBlurShader";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string BLUR_SHADER_NAME = "SpaceTime/PostProcess/SunShaft/DirectionalBlurShader";

        /// <summary>
        /// 
        /// </summary>
        public static readonly string FINAL_BLEND_SHADER_NAME = "SpaceTime/PostProcess/SunShaft/FinalBlendShader";

        /// <summary>
        /// 
        /// </summary>
        SunShaftsPass m_ShaftsPass;

        /// <summary>
        /// 
        /// </summary>
        public SunShaftsProperties props;

        /// <summary>
        /// 
        /// </summary>
        public RenderPassEvent shaftsPassEvent = RenderPassEvent.AfterRenderingTransparents;

        /// <summary>
        /// 
        /// </summary>
        public override void Create()
        {
            if (props == null)
            {
                props = new SunShaftsProperties();
            }
            
            m_ShaftsPass = new SunShaftsPass(props)
            {
                renderPassEvent = shaftsPassEvent,
                originRenderPassEvent = shaftsPassEvent,
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (props == null)
                return;

            props.CacheSunShafts();
            if (props.sunShafts == null)
                return;

            props.CopyVolumeProperty();

            var camera = renderingData.cameraData.camera;
            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
                return;

            RenderTargetIdentifier cameraColorTarget = renderer.cameraColorTarget;
            m_ShaftsPass.Setup(cameraColorTarget, RenderTargetHandle.CameraTarget);
            renderer.EnqueuePass(m_ShaftsPass);
        }
    }
}