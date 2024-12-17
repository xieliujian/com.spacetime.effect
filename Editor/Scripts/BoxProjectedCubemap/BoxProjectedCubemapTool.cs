using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace ST.Effect
{
    /// <summary>
    /// 
    /// </summary>
    public class BoxProjectedCubemapDirection
    {
        /// <summary>
        /// 
        /// </summary>
        static string _UseBoxCubeRefl = "_UseBoxCubeRefl";
        static string _BoxCubeReflCenter = "_BoxCubeReflCenter";
        static string _BoxCubeReflBoxMin = "_BoxCubeReflBoxMin";
        static string _BoxCubeReflBoxMax = "_BoxCubeReflBoxMax";

        /// <summary>
        /// 
        /// </summary>
        static Component[] s_CopiedComponents;

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("GameObject/BoxProjectedCubemapDirection_Copy", false, 20)]
        static void Copy()
        {
            s_CopiedComponents = Selection.activeGameObject.GetComponents<Component>();
        }

        /// <summary>
        /// 
        /// </summary>
        [MenuItem("GameObject/BoxProjectedCubemapDirection_Paste", false, 21)]
        static void Paste()
        {
            if (s_CopiedComponents == null)
                return;

            ReflectionProbeParam paremScript = null;
            foreach(var com in s_CopiedComponents)
            {
                if (com == null)
                    continue;

                var script = com as ReflectionProbeParam;
                if (script == null)
                    continue;

                paremScript = script;
            }

            if (paremScript == null)
                return;

            foreach (var targetGameObject in Selection.gameObjects)
            {
                if (!targetGameObject)
                    continue;

                MeshRenderer render = targetGameObject.GetComponent<MeshRenderer>();
                if (render == null)
                    continue;

                var mat = render.sharedMaterial;
                if (mat == null)
                    continue;

                bool dirty = false;

                if (mat.HasProperty(_UseBoxCubeRefl))
                {
                    mat.SetFloat(_UseBoxCubeRefl, 1);
                    dirty = true;
                }

                if (mat.HasProperty(_BoxCubeReflCenter))
                {
                    mat.SetVector(_BoxCubeReflCenter, paremScript.reflProbeCenter);
                    dirty = true;
                }

                if (mat.HasProperty(_BoxCubeReflCenter))
                {
                    mat.SetVector(_BoxCubeReflCenter, paremScript.reflProbeCenter);
                    dirty = true;
                }

                if (mat.HasProperty(_BoxCubeReflBoxMin))
                {
                    mat.SetVector(_BoxCubeReflBoxMin, paremScript.reflProbeBoxMin);
                    dirty = true;
                }

                if (mat.HasProperty(_BoxCubeReflBoxMax))
                {
                    mat.SetVector(_BoxCubeReflBoxMax, paremScript.reflProbeBoxMax);
                    dirty = true;
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(mat);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}
