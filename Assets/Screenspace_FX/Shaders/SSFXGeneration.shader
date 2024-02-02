Shader "Hypesio/SSFXGeneration"
{
    Properties
    {
        _AlphaMap ("Alpha Map", 2D) = "white" {}
        //_DurationDisapear("Duration disapear", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off
        Pass
        {
            // This pass is here to be sure we will only summon one particle per particle
            Name "DepthPrepassSSFX"
            Tags
            {
                "LightMode" = "DepthPrepassSSFX"
            }

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex vertDepthSSFX
            #pragma fragment fragDepthSSFX

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Screenspace_FX/Shaders/SSFXUtilsDepth.hlsl"

            ENDHLSL
  
        }

        Pass
        {
            // This pass will add all particle datas to an append buffer 
            // datas that will be used to summon the particles
            Name "OpaquePassSSFX"
            Tags
            {
                "LightMode" = "OpaquePassSSFX"
            }
            ZTest LEqual
            ZWrite Off

            HLSLPROGRAM

            #define OVERRIDE_MAT 1
            #pragma target 5.0
            #pragma vertex vertSSFXGeneration
            #pragma fragment fragSSFXGeneration

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Screenspace_FX/Shaders/SSFXGeneration.hlsl"

            ENDHLSL
  
        }
    }
}
