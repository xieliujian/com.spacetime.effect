# com.spacetime.effect

Unity 视觉效果库，基于 URP 提供盒子投影 Cubemap 与太阳光轴后处理效果。

## 功能模块

### [盒子投影 Cubemap](readme/BoxProjectedCubemap/BoxProjectedCubemap.md)
适用于矩形场景的 Cubemap 盒子投影，配合编辑器工具一键同步 ReflectionProbe 参数到材质，实现墙壁门窗等物件的精准反射投影。

### [SunShaft 光线](readme/SunShaft/SunShaft.md)
基于 URP ScriptableRendererFeature 的太阳光轴（体积光）后处理效果，支持 Volume 参数驱动、多级降采样、径向模糊与模板遮罩。

## 环境要求

- Unity 2020.3+
- Universal Render Pipeline (URP)
