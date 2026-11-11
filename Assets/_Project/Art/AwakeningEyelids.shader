Shader "BreathInTheRubble/Awakening Eyelids"
{
    Properties { _Opening("Eye opening", Range(0,1))=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay+100" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest Always Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half _Opening;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS:SV_POSITION; float4 screen:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            V Vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.screen=ComputeScreenPos(o.positionCS); return o;
            }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv=i.screen.xy/i.screen.w;
                float curve=abs(uv.y-.5)+pow(abs(uv.x-.5),2)*.9*(1-_Opening);
                float aperture=lerp(-.055,.80,_Opening);
                float lids=smoothstep(aperture-.035,aperture+.035,curve);
                float darkness=1-smoothstep(0,.22,_Opening);
                float alpha=max(lids,darkness)*(1-smoothstep(.82,1,_Opening));
                return half4(0,0,0,alpha);
            }
            ENDHLSL
        }
    }
}
