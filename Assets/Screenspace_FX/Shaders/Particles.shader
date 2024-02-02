// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hypesio/Particles"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SizeParticles("_SizeParticles", int) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
			HLSLPROGRAM
			#pragma target 5.0
			
			#pragma vertex vert
			#pragma fragment frag
			
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ParticlesCommons.cginc"

            struct VertexInput
            {
                float4 vertexPosition  : POSITION;
            };
			
			// Pixel shader input
			struct FragmentInput
			{
				float4 position : SV_POSITION;
				float3 color : COLOR;
			};
			
			uniform StructuredBuffer<ParticleDatas> _ParticlesDatasBuffer;
            //uniform float _SizeParticles;

			
			// Vertex shader
			FragmentInput vert(VertexInput i, uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
			{
				FragmentInput o = (FragmentInput)0;

                ParticleDatas particle = _ParticlesDatasBuffer[instance_id];
                float3 worldPosition = particle.worldPosition.xyz + (i.vertexPosition.xyz * particle.size);
                o.position = TransformWorldToHClip(worldPosition);
                o.color = particle.color;
				return o;
			}

			// Pixel shader
			half4 frag(FragmentInput i) : COLOR
			{
				return half4(i.color, 1.0);
			}
			
			ENDHLSL
        }
    }
}
