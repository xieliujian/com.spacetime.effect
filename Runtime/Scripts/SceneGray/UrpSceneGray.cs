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
        const float REFERENCE_SCREEN_WIDTH = 1920f;
        const float MIN_UI_SCALE = 0.5f;
        const float MAX_UI_SCALE = 2f;
        const float BASE_FONT_SIZE = 50f;
        const float BASE_BUTTON_FONT_SIZE = 36f;
        const float BASE_MARGIN = 20f;
        const float BASE_BUTTON_WIDTH = 280f;
        const float BASE_BUTTON_HEIGHT = 80f;
        const float BASE_LABEL_WIDTH = 600f;
        const float BASE_LABEL_HEIGHT = 100f;

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
            float scale = Mathf.Clamp(Screen.width / REFERENCE_SCREEN_WIDTH, MIN_UI_SCALE, MAX_UI_SCALE);
            float margin = BASE_MARGIN * scale;
            float buttonWidth = BASE_BUTTON_WIDTH * scale;
            float buttonHeight = BASE_BUTTON_HEIGHT * scale;

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = Color.white;
            labelStyle.fontSize = Mathf.RoundToInt(BASE_FONT_SIZE * scale);

            Rect labelRect = new Rect(margin, margin, BASE_LABEL_WIDTH * scale, BASE_LABEL_HEIGHT * scale);
            GUI.Label(labelRect, "运行时查看效果", labelStyle);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = Mathf.RoundToInt(BASE_BUTTON_FONT_SIZE * scale);

            Rect buttonRect = new Rect(
                Screen.width - margin - buttonWidth,
                margin,
                buttonWidth,
                buttonHeight);

            if (GUI.Button(buttonRect, "切换效果", buttonStyle))
            {
                SwitchEff();
            }
        }
    }
}