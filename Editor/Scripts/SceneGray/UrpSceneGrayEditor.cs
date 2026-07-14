using UnityEngine;
using UnityEditor;

namespace ST.Effect
{
    /// <summary>
    /// UrpSceneGray 的 Inspector：提供切换效果按钮。
    /// </summary>
    [CustomEditor(typeof(UrpSceneGray))]
    public class UrpSceneGrayEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var script = target as UrpSceneGray;
            if (script == null)
            {
                return;
            }

            if (GUILayout.Button("切换效果"))
            {
                script.SwitchEff();
                Repaint();
            }
        }
    }
}