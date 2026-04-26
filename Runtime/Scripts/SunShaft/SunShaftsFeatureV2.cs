
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

            m_ShaftsPass.Setup(renderer, RenderTargetHandle.CameraTarget);
            renderer.EnqueuePass(m_ShaftsPass);
        }
    }
}