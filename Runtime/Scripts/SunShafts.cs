using UnityEngine;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ST.Effect.URP.SunShaft
{
    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <c>LayerMask</c> value.
    /// </summary>
    [Serializable, System.Diagnostics.DebuggerDisplay(k_DebuggerDisplay)]
    public class RenderPassEventParameter : VolumeParameter<RenderPassEvent>
    {
        /// <summary>
        /// Creates a new <see cref="RenderPassEventParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public RenderPassEventParameter(RenderPassEvent value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    [Serializable, VolumeComponentMenu("Post-processing/SunShafts")]
    public class SunShafts : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Enable/Disable")]
        public BoolParameter on = new BoolParameter(false, false);

        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Enable/Disable")]
        public BoolParameter forceOn = new BoolParameter(false, false);

        /// <summary>
        /// 
        /// </summary>
        [Header("Render Pass Event")]
        public BoolParameter useRenderPassEvent = new BoolParameter(false);

        /// <summary>
        /// 
        /// </summary>
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.AfterRenderingSkybox);

        /// <summary>
        /// 
        /// </summary>
        [Header("Use Stencil Mask Tex")]
        public BoolParameter useStencilMaskTex = new BoolParameter(false);

        /// <summary>
        /// 
        /// </summary>
        [Header("Common params")]
        [Tooltip("SunShafts intensity")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1.5f, 0f, 5f);

        /// <summary>
        /// 
        /// </summary>
        [Header("Sun params")]
        public BoolParameter useSunLightColor = new BoolParameter(true);

        /// <summary>
        /// 
        /// </summary>
        [ColorUsage(false, true)]
        public ColorParameter shaftsColor = new ColorParameter(Color.black);

        /// <summary>
        /// 
        /// </summary>
        [Header("Sun Position")]
        public BoolParameter useSunPosition = new BoolParameter(false);

        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Can be used for fake Sun Source, which is differ from main Sun Light - source of shadows")]
        public Vector3Parameter sunPosition = new Vector3Parameter(Vector3.zero);

        /// <summary>
        /// 
        /// </summary>
        [Tooltip("SunShafts sunThresholdSky")]
        public ClampedFloatParameter sunThresholdSky = new ClampedFloatParameter(0.75f, 0f, 1f);

        /// <summary>
        /// 
        /// </summary>
        [Header("Blur params")]
        [Tooltip("SunShafts depthDownscalePow2")]
        public ClampedIntParameter depthDownscalePow2 = new ClampedIntParameter(3, 0, 4);

        /// <summary>
        /// 
        /// </summary>
        [Tooltip("SunShafts blurRadius")]
        public FloatParameter blurRadius = new FloatParameter(1.2f);

        /// <summary>
        /// 
        /// </summary>
        [Tooltip("SunShafts blurStepsCount")]
        public ClampedIntParameter blurStepsCount = new ClampedIntParameter(2, 1, 4);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsActive()
        {
            return (bool)on;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsTileCompatible() => false;
    }
}

