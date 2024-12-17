
using UnityEngine;

namespace ST.Effect.URP
{
    /// <summary>
    /// 
    /// </summary>
    public static class SunShaftUtil
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="shaderName"></param>
        public static void GetMaterial(ref Material mat, string shaderName)
        {
            if (mat == null || mat.shader == null)
            {
                if (mat != null)
                {
                    GameObject.DestroyImmediate(mat, true);
                    mat = null;
                }

                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    return;
                }

                mat = new Material(shader);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shader"></param>
        /// <param name="defaultShaderName"></param>
        /// <param name="usedShader"></param>
        /// <returns></returns>
        public static Material GetShaderMaterial(Shader shader, string defaultShaderName, out Shader usedShader)
        {
            if (shader)
            {
                usedShader = shader;
                return new Material(shader);
            }

            shader = Shader.Find(defaultShaderName);
            if (!shader)
            {
                usedShader = null;
                //Case of loading scene. On next frame all must be ok
                //Debug.LogError($"Can't find shader {defaultShaderName}");
                return null;
            }

            usedShader = shader;
            return new Material(shader);
        }
    }
}