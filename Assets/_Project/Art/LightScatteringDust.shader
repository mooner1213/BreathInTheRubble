Shader "BreathInTheRubble/Light Scattering Dust"
{
    Properties
    {
        [MainTexture] _BaseMap("Existing dust texture",2D)="white"{}
        [MainColor] _BaseColor("Dust tint and opacity",Color)=(.78,.77,.74,.9)
        _AmbientScatter("Very faint unlit visibility",Range(0,.02))=.0015
        _ScatteringGain("Light scattering",Float)=.2
        _SoftDistance("Surface intersection softness",Float)=.08
        _NearFade("Near eye fade distance",Float)=.18
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Scattering"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST; half4 _BaseColor;
                half _AmbientScatter, _ScatteringGain; float _SoftDistance, _NearFade;
            CBUFFER_END
            struct A { float4 positionOS:POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V
            {
                float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0;
                float2 uv:TEXCOORD1; half4 color:COLOR; float eyeDepth:TEXCOORD2; half fog:TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            V Vert(A i)
            {
                V o; UNITY_SETUP_INSTANCE_ID(i); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionWS=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS);
                o.uv=TRANSFORM_TEX(i.uv,_BaseMap); o.color=i.color*_BaseColor;
                o.eyeDepth=-TransformWorldToView(o.positionWS).z; o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            half3 Scatter(Light light,half3 view)
            {
                // Suspended dust scatters from either side; billboard normals must not turn smoke into dark cards.
                half mu=dot(light.direction,view);
                half phase=.65h+.65h*mu*mu;
                return light.color*min(light.distanceAttenuation,80.0)*light.shadowAttenuation*phase*_ScatteringGain;
            }
            float Hash(float2 p)
            {
                float3 q=frac(float3(p.xyx)*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);
            }
            float CloudNoise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            half4 Frag(V i):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float2 screenUV=GetNormalizedScreenSpaceUV(i.positionCS);
                float rawDepth=SampleSceneDepth(screenUV);
                float sceneDepth=LinearEyeDepth(rawDepth,_ZBufferParams);
                float soft=saturate((sceneDepth-i.eyeDepth)/max(.001,_SoftDistance));
                float nearFade=smoothstep(.045,max(.06,_NearFade),i.eyeDepth);
                half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                float cloud=CloudNoise(i.uv*5.3+i.positionWS.xz*.8+float2(_Time.y*.011,-_Time.y*.006));
                float2 centered=(i.uv-.5)*2;
                float feather=1-smoothstep(.12,lerp(.78,1.05,cloud),dot(centered,centered));
                half alpha=tex.a*i.color.a*soft*nearFade*feather*(.38+cloud*.95);
                clip(alpha-.0005h);
                half3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
                half3 illumination=_AmbientScatter.xxx;
                Light mainLight=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                illumination+=Scatter(mainLight,view);
                InputData inputData=(InputData)0; inputData.positionWS=i.positionWS; inputData.normalizedScreenSpaceUV=screenUV;
                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX) || USE_CLUSTER_LIGHT_LOOP
                uint count=GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(count)
                    Light localLight=GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1));
                    illumination+=Scatter(localLight,view);
                LIGHT_LOOP_END
                #endif
                half3 color=tex.rgb*i.color.rgb*illumination;
                // Dense air attenuates distance without adding a self-lit grey fog sheet.
                color=MixFog(color,i.fog);
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
