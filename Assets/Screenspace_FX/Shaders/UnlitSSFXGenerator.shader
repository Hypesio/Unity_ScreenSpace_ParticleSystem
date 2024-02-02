Shader "Hypesio/UnlitSSFXGenerator"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaMap("Texture", 2D) = "white" {}
        _ParticleLifetime("Particles life time", Float) = 2
        _SpawnRateParticles("Particle Spawn Rate", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Screenspace_FX/Shaders/SSFXGeneration.hlsl"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
        
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = TRANSFORM_TEX(v.uv, _AlphaMap);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 col = tex2D(_MainTex, i.uv);
                // Discard Pixel if needed
                if (IsPixelDiscard(i.uv2))
                    discard;

                return col;
            }
            ENDHLSL
        }

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
            #pragma target 5.0
            #pragma vertex vertSSFXGeneration
            #pragma fragment fragSSFXGeneration

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Screenspace_FX/Shaders/SSFXGeneration.hlsl"

            ENDHLSL
  
        }
    }
}
