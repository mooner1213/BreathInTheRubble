Shader "BreathInTheRubble/Head Impact Edges"
{
    Properties { _Color("Impact color", Color)=(0.8,0.015,0.008,1) _Opacity("Opacity",Range(0,0.65))=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Overlay+50" "RenderType"="Transparent"}
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
                half4 _Color; half _Opacity;
            CBUFFER_END
            struct A{float4 positionOS:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V{float4 positionCS:SV_POSITION;float4 screenPos:TEXCOORD0;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A i){V o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.screenPos=ComputeScreenPos(o.positionCS);return o;}
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 uv=i.screenPos.xy/i.screenPos.w;
                float edge=smoothstep(0.25,0.65,length((uv-0.5)*float2(1.15,1)));
                return half4(_Color.rgb,_Color.a*_Opacity*edge);
            }
            ENDHLSL
        }
    }
}
