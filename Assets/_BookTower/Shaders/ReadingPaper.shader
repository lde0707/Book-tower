Shader "BookTower/ReadingPaper"
{
 Properties {
  _BaseMap("Paper atlas",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1)
  _Rect("Atlas xy / size zw",Vector)=(0,0,1,1)
  _Emission("Paper glow",Float)=0 _Repeat("Repeat",Float)=1
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Cull Off
  Pass {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _Rect; half4 _BaseColor; float _Emission,_Repeat;
   CBUFFER_END
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;};
   V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=i.uv;return o;}
   half4 frag(V i):SV_Target {
    float2 u=lerp(saturate(i.uv),frac(i.uv),_Repeat);u=_Rect.xy+u*_Rect.zw;
    half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,u).rgb*_BaseColor.rgb;
    half3 n=normalize(i.normalWS);Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half3 light=max(SampleSH(n),half3(.018,.018,.02));
    light+=sun.color*saturate(dot(n,sun.direction))*sun.distanceAttenuation*sun.shadowAttenuation;
    #if defined(_ADDITIONAL_LIGHTS) || USE_CLUSTER_LIGHT_LOOP
    InputData inputData=(InputData)0;inputData.positionWS=i.positionWS;inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
    uint count=GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(count)
     Light l=GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1));light+=l.color*(.15+.85*saturate(dot(n,l.direction)))*l.distanceAttenuation*l.shadowAttenuation;
    LIGHT_LOOP_END
    #endif
    half ao=GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.positionCS)).indirectAmbientOcclusion;
    return half4(base*(light*ao+_Emission),1);
   }
   ENDHLSL
  }
  Pass {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma vertex vertDepth
   #pragma fragment fragDepth
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Rect; half4 _BaseColor; float _Emission,_Repeat;
   CBUFFER_END
   float4 vertDepth(float4 p:POSITION):SV_POSITION{return TransformObjectToHClip(p.xyz);}
   half4 fragDepth():SV_Target{return 0;}
   ENDHLSL
  }
  Pass {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex vertShadow
   #pragma fragment fragShadow
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Rect; half4 _BaseColor; float _Emission,_Repeat;
   CBUFFER_END
   float3 _LightDirection;float3 _LightPosition;
   struct AS {float4 p:POSITION;float3 n:NORMAL;};
   float4 vertShadow(AS i):SV_POSITION {
    float3 p=TransformObjectToWorld(i.p.xyz),n=TransformObjectToWorldNormal(i.n);
    #ifdef _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 dir=normalize(_LightPosition-p);
    #else
    float3 dir=_LightDirection;
    #endif
    return TransformWorldToHClip(ApplyShadowBias(p,n,dir));
   }
   half4 fragShadow():SV_Target{return 0;}
   ENDHLSL
  }
 }
}
