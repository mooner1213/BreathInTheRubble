Shader "BreathInTheRubble/Rescue Sun Shafts"
{
    Properties { _Strength("Sun scattering",Float)=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+50" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float _Strength;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS:SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz);return o;
            }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv=GetNormalizedScreenSpaceUV(i.positionCS);
                float depth=SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 end=ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                float3 origin=GetCameraPositionWS();
                float3 direction=normalize(end-origin);
                float distance=min(length(end-origin),2.6);
                Light sun=GetMainLight();
                float phase=.12+pow(saturate(dot(direction,sun.direction)),6)*1.4;
                float scatter=0;
                // Bounded, shadow-aware scattering: only sunlit portions of the view ray glow.
                [unroll] for(int step=0;step<12;step++)
                {
                    float t=(step+.5)/12.0;
                    float3 samplePositionWS=origin+direction*distance*t;
                    float shadow=MainLightRealtimeShadow(TransformWorldToShadowCoord(samplePositionWS));
                    scatter+=shadow*exp(-distance*t*.45);
                }
                half3 color=sun.color*(scatter/12)*distance*phase*.16*_Strength;
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
