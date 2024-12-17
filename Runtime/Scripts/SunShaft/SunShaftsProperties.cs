
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ST.Effect.URP
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class SunShaftsProperties
    {
        /// <summary>
        /// 摄像机与灯光的角度
        /// </summary>
        const float CAN_VISIBLE_RENDER_LIGHT_ANGLE = 30.0f;

        /// <summary>
        /// 摄像机与向上方向向量的角度
        /// </summary>
        const float CAN_VISIBLE_RENDER_UP_ANGLE = 70.0f;

        /// <summary>
        /// 
        /// </summary>
        [HideInInspector]
        public bool isOn = false;

        /// <summary>
        /// 
        /// </summary>
        [HideInInspector]
        public bool forceOn = false;

        /// <summary>
        /// 
        /// </summary>
        [Header("Common params")]
        public FilterMode filterMode = FilterMode.Bilinear;
        [Range(0, 5)]

        /// <summary>
        /// 
        /// </summary>
        public float intensity = 0.2f;

        /// <summary>
        /// 
        /// </summary>
        [Tooltip("LayerMask to render to depth normals texture")]
        public LayerMask normalsLayerMask = -1; //All by default

        /// <summary>
        /// 
        /// </summary>
        [Header("Sun params")] 
        public bool useSunLightColor = true;
        [ColorUsage(false, true)]
        public Color shaftsColor;

        /// <summary>
        /// 
        /// </summary>
        [HideInInspector]
        [Header("Sun Position")]
        public bool useSunPosition = false;

        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Can be used for fake Sun Source, which is differ from main Sun Light - source of shadows")]
        public Vector3 sunPosition;

        /// <summary>
        /// 
        /// </summary>
        [Range(0, 1)]
        public float sunThresholdSky = 0.75f;

        /// <summary>
        /// 
        /// </summary>
        [Range(0, 1)]
        public float sunThresholdDepth = 0.75f;

        /// <summary>
        /// 
        /// </summary>
        public float skyNoiseScale = 75f;

        /// <summary>
        /// 
        /// </summary>
        [Header("Blur params")]
        [Range(0, 4)]
        public int depthDownscalePow2 = 0;

        /// <summary>
        /// 
        /// </summary>
        public float blurRadius = 0.15f;

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        public float radiusDivider = 750f;

        /// <summary>
        /// 
        /// </summary>
        [Range(1, 4)]
        public int blurStepsCount = 2;

        /// <summary>
        /// 
        /// </summary>
        [HideInInspector]
        public bool useRenderPassEvent = false;

        /// <summary>
        /// 
        /// </summary>
        [HideInInspector]
        public RenderPassEvent renderPassEvent = new RenderPassEvent();

        /// <summary>
        /// 
        /// </summary>
        [HideInInspector]
        public bool useStencilMaskTex = false;

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        public Shader buildSkyShader;

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        public Shader blurShader;

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        public Shader finalBlendShader;

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        public Material buildSkyMaterial;

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        public Material blurMaterial;

        /// <summary>
        /// 
        /// </summary>
        [NonSerialized]
        public Material finalBlendMaterial;

        /// <summary>
        /// 
        /// </summary>
        SunShafts m_SunShafts;

        /// <summary>
        /// 
        /// </summary>
        public SunShafts sunShafts
        {
            get { return m_SunShafts; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool CanVolumeRender()
        {
            if (!forceOn)
            {
                return isOn;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void CopyVolumeProperty()
        {
            isOn = m_SunShafts.IsActive();
            forceOn = m_SunShafts.forceOn.value;

            useRenderPassEvent = m_SunShafts.useRenderPassEvent.value;
            renderPassEvent = m_SunShafts.renderPassEvent.value;

            useStencilMaskTex = m_SunShafts.useStencilMaskTex.value;

            intensity = m_SunShafts.intensity.value;

            useSunLightColor = m_SunShafts.useSunLightColor.value;
            shaftsColor = m_SunShafts.shaftsColor.value;

            useSunPosition = m_SunShafts.useSunPosition.value;
            sunPosition = m_SunShafts.sunPosition.value;

            sunThresholdSky = m_SunShafts.sunThresholdSky.value;

            depthDownscalePow2 = m_SunShafts.depthDownscalePow2.value;
            blurRadius = m_SunShafts.blurRadius.value;
            blurStepsCount = m_SunShafts.blurStepsCount.value;
        }

        /// <summary>
        /// 
        /// </summary>
        public void CacheSunShafts()
        {
            var stack = VolumeManager.instance.stack;
            if (stack != null)
            {
                m_SunShafts = stack.GetComponent<SunShafts>();
            }
        }

        /// <summary>
        /// 加载所有的材质
        /// </summary>
        public void CacheAllMaterial()
        {
            SunShaftUtil.GetMaterial(ref buildSkyMaterial, SunShaftsFeatureV2.BUILD_SKY_SHADER_NAME);
            SunShaftUtil.GetMaterial(ref blurMaterial, SunShaftsFeatureV2.BLUR_SHADER_NAME);
            SunShaftUtil.GetMaterial(ref finalBlendMaterial, SunShaftsFeatureV2.FINAL_BLEND_SHADER_NAME);
        }

        /// <summary>
        /// 是否太阳被渲染
        /// </summary>
        /// <returns></returns>
        public bool CanSunRender(Camera camera, out Vector3 _sunScreenPoint)
        {
            _sunScreenPoint = Vector3.zero;
            if (camera == null)
                return false;

            var lightpos = GetSunLightWorldPosition(camera);

            // 摄像机看不到太阳光, pass
            var sunScreenPoint = camera.WorldToViewportPoint(lightpos);
            _sunScreenPoint = sunScreenPoint;
            if (sunScreenPoint.z < 0f)
                return false;

            if (!forceOn)
            {
                // 摄像机方向和水平面大于30度才看见体积光
                var isinanglerender = IsInAngleRender(camera);
                if (!isinanglerender)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsVolumeLightSwitchOpen()
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        Vector3 GetLightDir()
        {
            Vector3 dir = Vector3.forward;

            if (RenderSettings.sun != null)
            {
                dir = RenderSettings.sun.gameObject.transform.forward;
            }

            return dir;
        }

        /// <summary>
        /// 
        /// </summary>
        Vector3 GetSunLightWorldPosition(Camera camera)
        {
            if (useSunPosition)
            {
                return sunPosition;
            }
            else
            {
                if (!RenderSettings.sun)
                {
                    return sunPosition;
                }
                else
                {
                    var dir = GetLightDir();

                    var fardis = 10000f;

                    var pos = -dir * fardis;
                    return pos;
                }
            }
        }

        /// <summary>
        /// 是否在角度中渲染
        /// </summary>
        /// <returns></returns>
        bool IsInAngleRender(Camera camera)
        {
            var lightdir = GetLightDir();
            var camforward = camera.transform.forward;
            float angle1 = Mathf.Abs(Vector3.Angle(-camforward, lightdir));
            float angle2 = Mathf.Abs(Vector3.Angle(camforward, Vector3.up));

            bool isInAngle1 = angle1 <= CAN_VISIBLE_RENDER_LIGHT_ANGLE;
            bool isInAngle2 = angle2 <= CAN_VISIBLE_RENDER_UP_ANGLE;
            return isInAngle1 || isInAngle2;
        }
    }
}