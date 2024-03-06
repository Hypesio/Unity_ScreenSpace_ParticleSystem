Shader "Hypesio/Particles"
{
    Properties
    {

    }
    SubShader
    {
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
				float4 color : TEXCOORD0;
			};
			
			uniform StructuredBuffer<ParticleDatas> _ParticlesDatasBuffer;
            //uniform float _SizeParticles;

			
			// Vertex shader
			FragmentInput vert(VertexInput i, uint vertex_id : SV_VertexID, uint instance_id : SV_InstanceID)
			{
				FragmentInput o = (FragmentInput)0;

                // Warning models matrix are invalid in case of DispatchIndirect
                ParticleDatas particle = _ParticlesDatasBuffer[instance_id];
                float3 worldPosition;
                
                // No bilboard 
                //worldPosition = particle.worldPosition.xyz + (i.vertexPosition.xyz * particle.size);
                //o.position = TransformWorldToHClip(worldPosition);

                // SphericalBilboarding
				float3 vpos = (i.vertexPosition.xyz  * particle.size);
				float4 worldCoord = particle.worldPosition;
				float4 viewPos = mul(UNITY_MATRIX_V, worldCoord) + float4(vpos, 0);
                worldPosition = mul(UNITY_MATRIX_I_V, viewPos);

                o.color = float4(particle.color, 1.0);
                o.position = TransformWorldToHClip(worldPosition);
                
				return o;
			}

			// Pixel shader
			half4 frag(FragmentInput i) : COLOR
			{
				return half4(i.color.rgb, 0);
			}
			
			ENDHLSL
        }
    }
}
