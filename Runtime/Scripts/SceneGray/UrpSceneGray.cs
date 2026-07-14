using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ST.Effect
{
    /// <summary>
    /// 通过 Volume ColorAdjustments 切换场景灰度效果。
    /// </summary>
    public class UrpSceneGray : MonoBehaviour
    {
        bool m_Gray;

        /// <summary>
        /// 切换灰度效果开/关。
        /// </summary>
        public void SwitchEff()
        {
            Volume volume = GetComponent<Volume>();
            if (volume == null)
            {
                return;
            }

            ColorAdjustments colorAdjust = null;
            volume.sharedProfile.TryGet<ColorAdjustments>(out colorAdjust);
            if (colorAdjust == null)
            {
                return;
            }

            m_Gray = !m_Gray;

            if (m_Gray)
            {
                colorAdjust.postExposure.value = SceneGrayDefine.s_GrayPostExposure;
                colorAdjust.contrast.value = SceneGrayDefine.s_GrayContrast;
                colorAdjust.hueShift.value = SceneGrayDefine.s_GrayHueShift;
                colorAdjust.saturation.value = SceneGrayDefine.s_GraySaturation;
            }
            else
            {
                colorAdjust.postExposure.value = SceneGrayDefine.s_NormalPostExposure;
                colorAdjust.contrast.value = SceneGrayDefine.s_NormalContrast;
                colorAdjust.hueShift.value = SceneGrayDefine.s_NormalHueShift;
                colorAdjust.saturation.value = SceneGrayDefine.s_NormalSaturation;
            }
        }

        void OnGUI()
        {
            GUIStyle fontStyle = new GUIStyle();
            fontStyle.normal.textColor = Color.white;
            fontStyle.fontSize = 50;

            GUI.Label(new Rect(0, 0, 600, 300), "运行时查看效果", fontStyle);
        }
    }
}