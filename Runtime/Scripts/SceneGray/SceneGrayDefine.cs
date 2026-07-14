namespace ST.Effect
{
    /// <summary>
    /// SceneGray 效果的常量定义，集中管理灰度开/关时的 ColorAdjustments 参数。
    /// </summary>
    public static class SceneGrayDefine
    {
        /// <summary>灰度开启：曝光。</summary>
        public static readonly float s_GrayPostExposure = -0.1f;

        /// <summary>灰度开启：对比度。</summary>
        public static readonly float s_GrayContrast = 40f;

        /// <summary>灰度开启：色相偏移。</summary>
        public static readonly float s_GrayHueShift = 0f;

        /// <summary>灰度开启：饱和度。</summary>
        public static readonly float s_GraySaturation = -100f;

        /// <summary>灰度关闭：曝光。</summary>
        public static readonly float s_NormalPostExposure = 0f;

        /// <summary>灰度关闭：对比度。</summary>
        public static readonly float s_NormalContrast = 0f;

        /// <summary>灰度关闭：色相偏移。</summary>
        public static readonly float s_NormalHueShift = 0f;

        /// <summary>灰度关闭：饱和度。</summary>
        public static readonly float s_NormalSaturation = 0f;
    }
}